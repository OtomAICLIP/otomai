using OtomAI.Core.Collections;

namespace OtomAI.Bot.Client.Context;

/// <summary>
/// Game server client context. Extends base with game-specific state.
/// Mirrors Bubble.D3.Bot's BotGameClientContext.
/// </summary>
public sealed class BotGameClientContext : BotClientContextBase
{
    public required BotGameClient GameClient { get; init; }
    public required string SessionToken { get; init; }
    public required int ServerId { get; init; }

    public AtomicQueue<Func<Task>> RequestQueue { get; } = new();
    public CancellationTokenSource? WorkCts { get; set; }
    public GameRuntimeState RuntimeState { get; } = new();

    public bool IsCharacterLoaded { get; set; }
    public bool IsMapLoaded { get; set; }
    public bool IsWorkStarted { get; set; }
}

/// <summary>
/// Mutable game runtime state container.
/// Mirrors Bubble.D3.Bot's GameRuntimeState with 85+ properties.
/// </summary>
public sealed class GameRuntimeState
{
    // Character identity
    public long CharacterId { get; set; }
    public string CharacterName { get; set; } = "";
    public int Level { get; set; }
    public int BreedId { get; set; }
    public int Sex { get; set; }

    // Position
    public long CurrentMapId { get; set; }
    public int CurrentCellId { get; set; }
    public int Direction { get; set; }

    // Stats
    public int Kamas { get; set; }
    public int ActionPoints { get; set; }
    public int MovementPoints { get; set; }
    public int LifePoints { get; set; }
    public int MaxLifePoints { get; set; }
    public int EnergyPoints { get; set; }
    public int MaxEnergyPoints { get; set; }
    public int Experience { get; set; }
    public int ExperienceNextLevel { get; set; }

    // Characteristics
    public int Strength { get; set; }
    public int Intelligence { get; set; }
    public int Chance { get; set; }
    public int Agility { get; set; }
    public int Wisdom { get; set; }
    public int Vitality { get; set; }
    public int StatsPoints { get; set; }
    public int SpellPoints { get; set; }

    // Combat
    public bool InFight { get; set; }
    public int FightTurn { get; set; }
    public long FightId { get; set; }
    public bool IsMyTurn { get; set; }

    // Server
    public int ServerId { get; set; }
    public string ServerName { get; set; } = "";

    // Movement
    public bool IsMoving { get; set; }
    public bool IsRiding { get; set; }

    // Inventory
    public int InventoryWeight { get; set; }
    public int MaxInventoryWeight { get; set; }
    public int PodPercent => MaxInventoryWeight > 0 ? InventoryWeight * 100 / MaxInventoryWeight : 0;

    // Guild / Party
    public long GuildId { get; set; }
    public string GuildName { get; set; } = "";
    public long PartyId { get; set; }

    // Trajet / Route
    public string? CurrentTrajet { get; set; }
    public int TrajetStepIndex { get; set; }

    // Treasure Hunt
    public bool InTreasureHunt { get; set; }
    public int TreasureHuntStep { get; set; }

    // Job
    public Dictionary<int, int> JobLevels { get; set; } = [];

    // Misc
    public bool InHavenBag { get; set; }
    public bool AutoPassEnabled { get; set; }
    public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;
}
