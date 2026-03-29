using OtomAI.Bot.Client;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Tracks monster groups on maps for fight target selection.
/// Mirrors Bubble.D3.Bot's GameMonsterDiscoveryService + MonsterDiscoveryRegistry.
/// </summary>
public sealed class GameMonsterDiscoveryService : GameClientServiceBase
{
    private readonly Dictionary<long, List<MonsterGroup>> _mapMonsters = [];

    public GameMonsterDiscoveryService(BotGameClient client) : base(client) { }

    public void RegisterMonsterGroup(long mapId, MonsterGroup group)
    {
        if (!_mapMonsters.TryGetValue(mapId, out var groups))
        {
            groups = [];
            _mapMonsters[mapId] = groups;
        }
        groups.Add(group);
    }

    public void ClearMap(long mapId)
    {
        _mapMonsters.Remove(mapId);
    }

    public List<MonsterGroup> GetMonsterGroups(long mapId)
    {
        return _mapMonsters.GetValueOrDefault(mapId) ?? [];
    }
}

public sealed class MonsterGroup
{
    public long Id { get; set; }
    public int CellId { get; set; }
    public List<MonsterInGroup> Monsters { get; set; } = [];
    public int TotalLevel => Monsters.Sum(m => m.Level);
}

public sealed class MonsterInGroup
{
    public int MonsterId { get; set; }
    public int Level { get; set; }
}
