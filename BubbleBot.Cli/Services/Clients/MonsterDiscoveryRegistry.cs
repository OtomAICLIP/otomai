using System.Collections.Concurrent;

namespace BubbleBot.Cli.Services.Clients;

internal sealed class MonsterDiscoveryRegistry
{
    private static readonly ConcurrentDictionary<long, ConcurrentDictionary<int, byte>> LoggedByMap = new();

    public static bool TryMarkLogged(long mapId, int monsterId)
    {
        var monsters = LoggedByMap.GetOrAdd(mapId, _ => new ConcurrentDictionary<int, byte>());
        return monsters.TryAdd(monsterId, 0);
    }
}
