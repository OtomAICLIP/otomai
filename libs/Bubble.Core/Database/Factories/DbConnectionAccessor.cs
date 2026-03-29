using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Data;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Bubble.Core.Database.Batchs;
using Bubble.Core.Database.Interceptors;
using Bubble.Core.Extensions;
using Bubble.Core.Kernel;
using Bubble.Core.Services;
using Npgsql;
using Serilog;

namespace Bubble.Core.Database.Factories;

public sealed class DbConnectionAccessor : Singleton<DbConnectionAccessor>
{
    private readonly ConcurrentDictionary<DatabaseTypes, Func<NpgsqlConnection>> _connections;
    private readonly SemaphoreSlim _semaphore = new(15, 15);
    
    public bool IsInitialized => !_connections.IsEmpty;

    private static FrozenDictionary<Type, (DatabaseTypes DbType, string DbName)> DbTypes { get; set; } = null!;

    public DbConnectionAccessor()
    {
        _connections = new ConcurrentDictionary<DatabaseTypes, Func<NpgsqlConnection>>();

        if (Configuration.Instance.GetConnectionString("Login") is { } loginConnectionString)
            _connections.TryAdd(DatabaseTypes.Login, () => new NpgsqlConnection(loginConnectionString));

        if (Configuration.Instance.GetConnectionString("Static") is { } staticConnectionString)
            _connections.TryAdd(DatabaseTypes.Static, () => new NpgsqlConnection(staticConnectionString));

        if (Configuration.Instance.GetConnectionString("Dynamic") is { } dynamicConnectionString)
            _connections.TryAdd(DatabaseTypes.Dynamic, () => new NpgsqlConnection(dynamicConnectionString));
        
        if (Configuration.Instance.GetConnectionString("StaticData") is { } staticDataConnectionString)
            _connections.TryAdd(DatabaseTypes.StaticData, () => new NpgsqlConnection(staticDataConnectionString));

    }

    private string BuildWhereClause(params (string Name, object? Value)[] parameters)
    {
        return string.Join(" AND ", parameters.Select(x => $"{x.Name.ToSnakeCase()} {(x.Value == null ? "IS NULL": $"= @{x.Name}")}"));
    }

    public NpgsqlConnection CreateConnection(DatabaseTypes type)
    {
        if (!_connections.TryGetValue(type, out var connectionFactory))
            throw new Exception($"No connection string found for database type: {type}.");

        return connectionFactory();
    }
    
    public async Task ExecuteAsync(string sql)
    {
        await _semaphore.WaitAsync();

        try
        {
            await using var connection = CreateConnection(DatabaseTypes.StaticData);

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            await command.PrepareAsync();
            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ExecuteBatchsAsync(BatchStatements statements)
    {
        await _semaphore.WaitAsync();

        try
        {
            var statementsCount = statements.Statements.Count;

            Log.Information("Execute batch of {StatementsCount} statements", statementsCount);

            try
            {
                var groupByDbType = statements.Statements.GroupBy(x => DbTypes[x.EntityType].DbType);

                var connections = new Dictionary<DatabaseTypes, NpgsqlConnection>();

                Log.Information("Removing duplicates of batch statements");
                statements.RemoveDuplicates();
                Log.Information("Removed {StatementsCount} of batch statements", statementsCount - statements.Statements.Count);

                foreach (var group in groupByDbType)
                {
                    Log.Information("Execute batch of {StatementsCount} statements for {DbType}", group.Count(), group.Key);

                    var cachedSql = new Dictionary<(Type, BatchStatementMode), string>();

                    try
                    {
                        if (!connections.TryGetValue(group.Key, out var connection))
                        {
                            connections.Add(group.Key, connection = CreateConnection(group.Key));

                            if (connection.State is not ConnectionState.Open)
                            {
                                Log.Information("Open connection for {DbType}", group.Key);
                                await connection.OpenAsync();
                                Log.Information("Opened connection for {DbType}", group.Key);
                            }
                        }

                        Log.Information("Create batch for {DbType}", group.Key);

                        var batch = connection.CreateBatch();

                        var count = 0;

                        foreach (var statement in statements.Statements)
                        {
                            if (statement.EntityRecord is IPreSaveInterceptor preInterceptor)
                                preInterceptor.BeforeSave();

                            if (statement is { Mode: BatchStatementMode.Insert, EntityRecord.IsNew: false })
                            {
                                Log.Warning("Ignoring insert statement for {EntityType} because it is not a new record", statement.EntityType.Name);
                                continue;
                            }

                            if (statement.Mode is BatchStatementMode.Update && !statement.EntityRecord.IsDirty())
                            {
                                Log.Warning("Ignoring update statement for {EntityType} because it is not a dirty record", statement.EntityType.Name);
                                continue;
                            }

                            if (statement is { Mode: BatchStatementMode.Delete, EntityRecord.MustBeDeleted: false })
                            {
                                Log.Warning("Ignoring delete statement for {EntityType} because it is not a marked as deleted record", statement.EntityType.Name);
                                continue;
                            }

                            if (!cachedSql.TryGetValue((statement.EntityType, statement.Mode), out var sql))
                            {
                                sql = (statement.Mode, statement.EntityRecord) switch
                                {
                                    (BatchStatementMode.Insert, var record) => record.GetInsertSql(),
                                    (BatchStatementMode.Update, var record) => record.GetUpdateSql(),
                                    (BatchStatementMode.Delete, var record) => record.GetRemoveSql(),
                                    _ => throw new Exception("Unknown batch statement mode or record entity type.")
                                };

                                cachedSql.Add((statement.EntityType, statement.Mode), sql);
                            }

                            if (statement.EntityRecord.MustBeDeleted && statement.Mode is not BatchStatementMode.Delete)
                            {
                                Log.Warning("Ignoring statement for {EntityType} because it is marked as deleted but we are not in delete mode", statement.EntityType.Name);
                                continue;
                            }

                            statement.EntityRecord.ResetDirty();

                            var parameters = statement.EntityRecord.GetParameters();

                            var command = new NpgsqlBatchCommand(sql);

                            command.Parameters.AddRange(parameters);

                            batch.BatchCommands.Add(command);

                            if (++count >= statements.LimitExecution)
                            {
                                Log.Information("Execute batch of {Count} statements for {DbType}", count, group.Key);

                                batch.Timeout = 999;

                                await batch.PrepareAsync();
                                await batch.ExecuteNonQueryAsync();

                                Log.Information("Executed batch of {Count} statements for {DbType}", count, group.Key);

                                count = 0;
                                batch.BatchCommands.Clear();
                            }
                        }

                        Log.Information("Execute batch of {Count} statements pass two for {DbType}", count, group.Key);
                        await batch.PrepareAsync();
                        await batch.ExecuteNonQueryAsync();
                        Log.Information("Executed batch of {Count} statements pass two for {DbType}", count, group.Key);

                        batch.Timeout = 999;

                        foreach (var statement in statements.Statements)
                            if (statement.EntityRecord is IPostSaveInterceptor postInterceptor)
                                postInterceptor.AfterSave();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Error while executing batch, could not save batch of {StatementsCount} with entity type {EntityType}, saving the records one by one",
                            statements.Statements.Count, statements.Statements.FirstOrDefault()?.EntityType.Name);

                        if (!connections.TryGetValue(group.Key, out var connection))
                        {
                            connections.Add(group.Key, connection = CreateConnection(group.Key));

                            if (connection.State is not ConnectionState.Open)
                                await connection.OpenAsync();
                        }

                        await ExecuteRecoveryAsync(connection, statements, cachedSql);
                    }
                }

                Log.Information("[DEBUG-DB]: {StatementsCount} statements executed", statementsCount);

                foreach (var connection in connections.Values)
                    if (connection.State is ConnectionState.Open)
                        await connection.CloseAsync();
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while executing batch, could not save batch of {StatementsCount}.", statementsCount);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ExecuteRecoveryAsync(NpgsqlConnection connection, BatchStatements statements, Dictionary<(Type, BatchStatementMode), string> cachedSql)
    {
        await _semaphore.WaitAsync();

        try
        {
            var statementIndex = 0;

            foreach (var statement in statements.Statements)
            {
                if (!cachedSql.TryGetValue((statement.EntityType, statement.Mode), out var sql))
                {
                    sql = (statement.Mode, statement.EntityRecord) switch
                    {
                        (BatchStatementMode.Insert, var record) => record.GetInsertSql(),
                        (BatchStatementMode.Update, var record) => record.GetUpdateSql(),
                        (BatchStatementMode.Delete, var record) => record.GetRemoveSql(),
                        _ => throw new Exception("Unknown batch statement mode or record entity type.")
                    };

                    cachedSql.Add((statement.EntityType, statement.Mode), sql);
                }

                if (statement.EntityRecord.MustBeDeleted && statement.Mode is not BatchStatementMode.Delete)
                    continue;

                statement.EntityRecord.ResetDirty();

                try
                {
                    var command = new NpgsqlCommand(sql, connection);

                    var parameters = statement.EntityRecord.GetParameters();

                    command.Parameters.AddRange(parameters);

                    await command.PrepareAsync();
                    await command.ExecuteNonQueryAsync();
                }
                catch (Exception e)
                {
                    Log.Error(e, "Error while executing batch, recovery could not save statement {Sql} for {@Record}.", sql, statement.EntityRecord);

                    if (!Directory.Exists("recovery"))
                        Directory.CreateDirectory("recovery");

                    var json = JsonSerializer.Serialize(statement.EntityRecord);

                    await File.WriteAllTextAsync($"recovery/{statement.EntityType.Name}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{statementIndex++}.json", json);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Initialize(IDictionary<Type, (DatabaseTypes DbType, string DbName)> dbTypes)
    {
        DbTypes = dbTypes.ToFrozenDictionary();
    }

    public async Task InsertAsync<T>(T record)
        where T : class, IEntityRecord<T>, IEntityProperties
    {
        await _semaphore.WaitAsync();

        try
        {
            if (record.MustBeDeleted)
                return;

            await using var connection = CreateConnection(T.DatabaseType);

            await connection.OpenAsync();

            var cmd = record.GetInsertSql();

            if (T.HasAutoGeneratedField)
                cmd += " RETURNING " + T.AutoGeneratedField;

            await using var command = new NpgsqlCommand(cmd, connection);

            if (record is IPreSaveInterceptor preInterceptor)
                preInterceptor.BeforeSave();

            foreach (var parameter in record.GetParameters())
                command.Parameters.Add(parameter);

            await command.PrepareAsync();

            if (T.HasAutoGeneratedField)
                record.SetAutoGeneratedKey(await command.ExecuteScalarAsync());
            else
                await command.ExecuteNonQueryAsync();

            if (record is IPostSaveInterceptor postInterceptor)
                postInterceptor.AfterSave();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<bool> ExistAsync<T>(params (string Name, object? Value)[] parameters)
        where T : class, IEntityRecord<T>
    {
        await _semaphore.WaitAsync();

        try
        {
            await using var connection = CreateConnection(T.DatabaseType);

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {T.TableName} WHERE {BuildWhereClause(parameters)}", connection);

            foreach (var parameter in parameters)
            {
                if(parameter.Value != null)
                     command.Parameters.AddWithValue(parameter.Name, parameter.Value);
            }
            
            await command.PrepareAsync();

            return (long)(await command.ExecuteScalarAsync() ?? 0) > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<TB> MaxAsync<TA, TB>(string? name = null)
        where TA : class, IEntityRecord<TA>
        where TB : struct, INumber<TB>
    {
        await _semaphore.WaitAsync();

        try
        {
            await using var connection = CreateConnection(TA.DatabaseType);

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand($"SELECT MAX({name ?? TA.AutoGeneratedField}) FROM {TA.TableName}", connection);
            await command.PrepareAsync();

            return TB.Parse((await command.ExecuteScalarAsync())?.ToString() ?? "0", CultureInfo.CurrentCulture);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IList<T>> QueryAsync<T>(params (string Name, object? Value)[] parameters)
        where T : class, IEntityRecord<T>
    {
        await _semaphore.WaitAsync();

        try
        {
            await using var connection = CreateConnection(T.DatabaseType);

            await connection.OpenAsync();

            var sql = parameters.Length == 0
                ? $"SELECT * FROM {T.TableName}"
                : $"SELECT * FROM {T.TableName} WHERE {BuildWhereClause(parameters)}";

            Log.Information("[DEBUG-DB]: {Sql}", sql);

            await using var command = new NpgsqlCommand(sql, connection);

            foreach (var parameter in parameters)
            {
                if(parameter.Value != null)
                {
                    Log.Information("[DEBUG-DB]: {Name} = {Value}", parameter.Name, parameter.Value);
                    command.Parameters.AddWithValue(parameter.Name, parameter.Value);
                }
            }
            
            await command.PrepareAsync();
            await using var reader = await command.ExecuteReaderAsync();

            var records = new List<T>();

            while (await reader.ReadAsync())
                records.Add(T.Map(reader));

            foreach (var interceptor in records.OfType<IQueryInterceptor>())
                interceptor.OnQuery();

            return records;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<T?> QueryFirstOrDefaultAsync<T>(params (string Name, object? Value)[] parameters)
        where T : class, IEntityRecord<T>
    {
        await _semaphore.WaitAsync();

        try
        {
            await using var connection = CreateConnection(T.DatabaseType);

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand($"SELECT * FROM {T.TableName} WHERE {BuildWhereClause(parameters)}", connection);

            foreach (var parameter in parameters)
            {
                if(parameter.Value != null)
                    command.Parameters.AddWithValue(parameter.Name, parameter.Value);
            }
            
            await command.PrepareAsync();
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var record = T.Map(reader);

                if (record is IQueryInterceptor interceptor)
                    interceptor.OnQuery();

                return record;
            }

            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RemoveAllAsync<T>()
        where T : class, IEntityRecord<T>
    {
        await _semaphore.WaitAsync();

        try
        {
            await using var connection = CreateConnection(T.DatabaseType);

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand($"DELETE FROM {T.TableName}", connection);
            await command.PrepareAsync();

            await command.ExecuteNonQueryAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public async Task<bool> RemoveAsync<T>(T record, string? where = null, params (string Name, object? Value)[]? parameters)
        where T : class, IEntityRecord<T>
    {
        await _semaphore.WaitAsync();

        try
        {
            await using var connection = CreateConnection(T.DatabaseType);

            await connection.OpenAsync();

            var sql = where is null
                ? record.GetRemoveSql()
                : $"DELETE FROM {T.TableName} WHERE {where}";

            await using var command = new NpgsqlCommand(sql, connection);

            if (parameters is not null)
            {
                foreach (var parameter in parameters)
                {
                    if (parameter.Value != null)
                        command.Parameters.AddWithValue(parameter.Name, parameter.Value);
                }
            }
            else if (T.HasAutoGeneratedField == false)
            {
                throw new Exception("AutoGeneratedField is required for RemoveAsync without parameters");
            }           
            else
            {
                command.Parameters.AddWithValue(T.AutoGeneratedField, record.GetAutoGeneratedKey());
            }
            
            await command.PrepareAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> RemoveRangeAsync<T>()
        where T : class, IEntityRecord<T>
    {
        await _semaphore.WaitAsync();

        try
        {
            await using var connection = CreateConnection(T.DatabaseType);

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand("DROP TABLE {T.TableName}", connection);
            await command.PrepareAsync();

            return await command.ExecuteNonQueryAsync() > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> RemoveRangeAsync<T>(IEnumerable<T> records)
        where T : class, IEntityRecord<T>
    {
        await _semaphore.WaitAsync();

        try
        {
            var entityRecords = records as T[] ?? records.ToArray();

            if (entityRecords.Length == 0)
                return false;

            var ids = entityRecords.Select(x => x.GetAutoGeneratedKey()).ToArray();

            var parameters = ids.Select((_, index) => $"@p{index}").ToArray();

            var sql = $"DELETE FROM {T.TableName} WHERE {T.AutoGeneratedField} IN ({string.Join(", ", parameters)})";

            await using var connection = CreateConnection(T.DatabaseType);

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(sql, connection);

            for (var i = 0; i < ids.Length; i++)
                command.Parameters.AddWithValue($"p{i}", ids[i]);

            await command.PrepareAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ResetSequenceAsync<T>()
        where T : class, IEntityRecord<T>
    {
        await _semaphore.WaitAsync();

        try
        {
            await using var connection = CreateConnection(T.DatabaseType);

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand($"ALTER SEQUENCE {T.SequenceName} RESTART WITH 1", connection);
            await command.PrepareAsync();

            return (long)(await command.ExecuteScalarAsync() ?? 0) > 0;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveAsync<T>(T record)
        where T : class, IEntityRecord<T>, IEntityProperties
    {
        await _semaphore.WaitAsync();

        try
        {
            if (record is IPreSaveInterceptor preInterceptor)
                preInterceptor.BeforeSave();

            if (record.MustBeDeleted)
                await RemoveAsync(record);
            else if (record is { MustBeDeleted: false, IsNew: true })
            {
                await InsertAsync(record);
                record.IsNew = false;
                record.ResetDirty();
            }
            else if (record is { MustBeDeleted: false, IsNew: false } && record.IsDirty())
            {
                await UpdateAsync(record);
                record.ResetDirty();
            }

            if (record is IPostSaveInterceptor postInterceptor)
                postInterceptor.AfterSave();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task UpdateAsync<T>(T record)
        where T : class, IEntityRecord<T>, IEntityProperties
    {
        await _semaphore.WaitAsync();

        try
        {
            if (record.MustBeDeleted)
                return;

            await using var connection = CreateConnection(T.DatabaseType);

            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(record.GetUpdateSql(), connection);

            if (record is IPreSaveInterceptor preInterceptor)
                preInterceptor.BeforeSave();

            foreach (var parameter in record.GetParameters())
                command.Parameters.Add(parameter);

            await command.PrepareAsync();
            await command.ExecuteNonQueryAsync();

            if (record is IPostSaveInterceptor postInterceptor)
                postInterceptor.AfterSave();

            record.ResetDirty();
        }
        finally
        {
            _semaphore.Release();
        }
    }
}