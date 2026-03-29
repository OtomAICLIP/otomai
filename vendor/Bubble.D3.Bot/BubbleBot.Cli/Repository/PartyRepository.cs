using System.Collections.Concurrent;
using Bubble.Core.Services;

namespace BubbleBot.Cli.Repository;

public class PartyRepository : Singleton<PartyRepository>
{
    private readonly ConcurrentDictionary<int, PartyManager> _parties = new();

    public PartyManager GetOrCreatePartyManager(int serverId)
    {
        return _parties.GetOrAdd(serverId, _ => new PartyManager());
    }
    
    public long GetAvailablePlayerId(int serverId)
    {
        return GetOrCreatePartyManager(serverId).GetAvailablePlayerId();
    }
}