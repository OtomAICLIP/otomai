using Bubble.Core.Datacenter;
using Bubble.Core.Datacenter.Datacenter.Server;
using Bubble.Core.Datacenter.Datacenter.World;
using Bubble.Core.Services;
using Serilog;

namespace BubbleBot.Cli.Repository;

public class ServerRepository : Singleton<ServerRepository>
{
    private Dictionary<int, Servers> _servers;
    
    public ServerRepository()
    {
        _servers = new Dictionary<int, Servers>();
    }

    public void Initialize()
    {
        _servers = (DatacenterService.Load<Servers>()).Values.ToDictionary(x => x.Id);
        
        Log.Logger.Information($"Loaded {_servers.Count} servers.");
        
        foreach (var server in _servers.Values)
        {
            Log.Logger.Information($"Server {server.Id} - {server.Name}");
        }
    }
    
    public Servers? GetServer(int id)
    {
        return _servers.TryGetValue(id, out var server) ? server : null;
    }
}