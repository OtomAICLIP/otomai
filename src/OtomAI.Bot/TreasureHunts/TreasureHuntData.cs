namespace OtomAI.Bot.TreasureHunts;

/// <summary>
/// Treasure hunt state and step tracking.
/// Mirrors Bubble.D3.Bot's TreasureHuntData.
/// </summary>
public sealed class TreasureHuntData
{
    public long HuntId { get; set; }
    public TreasureHuntState State { get; set; } = TreasureHuntState.Idle;
    public int CurrentStep { get; set; }
    public int TotalSteps { get; set; }
    public List<ClueStep> Steps { get; set; } = [];
    public long StartMapId { get; set; }
    public int FightsInHunt { get; set; }
    public DateTime StartedAt { get; set; }
    public GiveUpReason? GaveUpReason { get; set; }
}

public sealed class ClueStep
{
    public int StepIndex { get; set; }
    public int ClueId { get; set; }
    public int Direction { get; set; } // 0=N, 2=E, 4=S, 6=W
    public long? TargetMapId { get; set; }
    public bool Completed { get; set; }
}

public enum TreasureHuntState
{
    Idle,
    WaitingForClue,
    NavigatingToClue,
    Digging,
    Fighting,
    Completed,
    GaveUp,
}

public enum GiveUpReason
{
    CantFindClue,
    TooManyFights,
    NavigationFailed,
    Timeout,
}
