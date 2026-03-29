namespace OtomAI.Bot.Client.Context;

/// <summary>
/// Kolosseum (PvP arena) server client context.
/// Mirrors Bubble.D3.Bot's BotKoliClientContext.
/// </summary>
public sealed class BotKoliClientContext : BotClientContextBase
{
    public required BotKoliClient KoliClient { get; init; }
    public required string SessionToken { get; init; }
    public KoliRuntimeState RuntimeState { get; } = new();
}

/// <summary>
/// Koli-specific runtime state. Mirrors Bubble.D3.Bot's KoliRuntimeState.
/// </summary>
public sealed class KoliRuntimeState
{
    public long FightId { get; set; }
    public int FightTurn { get; set; }
    public bool IsMyTurn { get; set; }
    public bool InFight { get; set; }
    public int CurrentCellId { get; set; }
    public long CurrentMapId { get; set; }
    public int ActionPoints { get; set; }
    public int MovementPoints { get; set; }
}
