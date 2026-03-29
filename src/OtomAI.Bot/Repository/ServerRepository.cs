using Serilog;

namespace OtomAI.Bot.Repository;

/// <summary>
/// Game server registry (singleton). Mirrors Bubble.D3.Bot's ServerRepository.
/// </summary>
public sealed class ServerRepository
{
    private static readonly Lazy<ServerRepository> _instance = new(() => new ServerRepository());
    public static ServerRepository Instance => _instance.Value;

    private readonly Dictionary<int, ServerRecord> _servers = [];
    private ServerRepository() { }

    public void Register(ServerRecord server) => _servers[server.Id] = server;
    public ServerRecord? Get(int serverId) => _servers.GetValueOrDefault(serverId);
    public IEnumerable<ServerRecord> GetAll() => _servers.Values;
}

public sealed class ServerRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; }
    public int Status { get; set; }
    public int CharacterCount { get; set; }
}
