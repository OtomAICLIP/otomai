using Bubble.Core.Datacenter.Datacenter.WorldGraph;
using Bubble.Shared.Protocol;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Fight;
using BubbleBot.Cli.Services.Maps;

namespace BubbleBot.Cli.Services.Clients;

internal sealed class KoliRuntimeState
{
    public BotGameClient? GameClient { get; set; }
    public Map? Map { get; set; }
    public WorldPath WorldPath { get; } = new();
    public List<WorldGraphEdge> AutoPath { get; set; } = [];
    public int AutoPathIndex { get; set; }
    public bool IsInFight { get; set; } = true;
    public long AutoPathEndMapId { get; set; }
    public FightActor? Fighter { get; set; }
    public List<SpellWrapper> Spells { get; set; } = [];
    public CharacterInfo Info { get; set; } = new();
    public MapComplementaryInformationEvent? MapCurrentEvent { get; set; }
    public bool NeedNextContext { get; set; }
    public bool Connected { get; set; } = true;
    public long ConnectedAt { get; set; }
    public bool IsSpecial { get; set; }
    public DateTime LastReconnect { get; set; } = DateTime.MinValue;
    public string ServerPrefix { get; set; } = string.Empty;
    public FightInfo? FightInfo { get; set; }
    public bool AutoPass { get; set; }
    public bool IsDisconnectionPlanned { get; set; }
    public bool IsKoli { get; set; }
}
