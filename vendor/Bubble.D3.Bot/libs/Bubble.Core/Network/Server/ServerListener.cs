using System.Collections.Concurrent;
using Bubble.Core.Network.Internal;
using Bubble.Core.Network.Transport.Sockets;
using Bubble.Core.Network.Transport.Sockets.Internal;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bubble.Core.Network.Server;

internal sealed class ServerListener
{
    private readonly SocketConnectionDelegate _application;
    private readonly ConcurrentDictionary<long, (ServerConnection Connection, Task ExecutionTask)> _connections;
    private readonly ILogger _logger;
    private readonly Task _shutdownTask;

    public SocketConnectionListener Listener { get; }

    public Task? ExecutionTask { get; private set; }

    public ServerListener(SocketConnectionListener listener, SocketConnectionDelegate application, Task? shutdownTask = default, ILogger? logger = default)
    {
        _connections = new ConcurrentDictionary<long, (ServerConnection Context, Task ExecutionTask)>();
        _shutdownTask = shutdownTask ?? Task.CompletedTask;
        _logger = logger ?? NullLogger.Instance;
        _application = application;
        Listener = listener;
    }

    private IDisposable? BeginConnectionScope(SocketConnection transport)
    {
        return _logger.IsEnabled(LogLevel.Critical)
            ? _logger.BeginScope(new ConnectionLogScope(transport.ConnectionId))
            : null;
    }

    private async Task RunListenerAsync()
    {
        await using var listener = Listener;

        _logger.LogDebug("Now listening on: {EndPoint}", listener.EndPoint);

        for (long id = 0;; id++)
            try
            {
                var connection = await listener.AcceptAsync().ConfigureAwait(false);
                // Null means we don't have anymore connections
                if (connection == default) break;

                var networkConnection = new ServerConnection(id, connection, _logger);
                _connections[id] = (networkConnection, StartConnectionAsync(networkConnection, _application));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Stopped accepting connections on network server at '{endpoint}'", listener.EndPoint);
                break;
            }

        _logger.LogDebug("Stopped listening on: {EndPoint}", listener.EndPoint);

        // Don't shut down connections until entire server is shutting down
        await _shutdownTask.ConfigureAwait(false);

        // Give connections a chance to close gracefully
        var tasks = new List<Task>(_connections.Count);

        foreach (var (_, (connection, task)) in _connections)
        {
            connection.RequestClose();
            tasks.Add(task);
        }

        if (await Task.WhenAll(tasks).TimeoutAfter(TimeSpan.FromSeconds(5)).ConfigureAwait(false))
            return;

        // If they didn't, abort try via Abort()
        const string serverStopped = "The connection was aborted because the server was stopped";

        foreach (var (_, (connection, _)) in _connections)
            connection.Transport.Abort(new ConnectionAbortedException(serverStopped));

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public void Start()
    {
        ExecutionTask = RunListenerAsync();
    }

    private async Task StartConnectionAsync(ServerConnection connection, SocketConnectionDelegate execute)
    {
        await Task.Yield();

        var transport = connection.Transport;

        using var scope = BeginConnectionScope(connection.Transport);

        const string acceptedMessage = "Connection with Id={ConnectionId} has successfully been accepted on network server at '{EndPoint}'";

        _logger.LogDebug(acceptedMessage, transport.ConnectionId, Listener.EndPoint);

        try
        {
            await execute(transport).ConfigureAwait(false);
        }
        catch (ConnectionAbortedException)
        {
            /* Don't let connection aborted exceptions out */
        }
        catch (ConnectionResetException)
        {
            /* Don't let connection reset exceptions out */
        }
        catch (Exception e)
        {
            const string errorMessage = "Unexpected exception caught from connection with Id={ConnectionId} on network server at '{EndPoint}'";

            _logger.LogError(e, errorMessage, transport.ConnectionId, Listener.EndPoint);
        }
        finally
        {
            await connection.FireOnCompletedAsync().ConfigureAwait(false);
            await transport.DisposeAsync().ConfigureAwait(false);

            // Remove the connection from tracking
            _connections.TryRemove(connection.Id, out _);

            const string completedMessage = "Connection with Id={ConnectionId} has successfully been completed on network server at '{EndPoint}'";

            _logger.LogDebug(completedMessage, transport.ConnectionId, Listener.EndPoint);
        }
    }

    public void TickHeartbeat()
    {
        foreach (var (_, (connection, _)) in _connections)
            connection.TickHeartbeat();
    }
}