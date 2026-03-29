namespace OtomAI.Bot.Fight;

/// <summary>
/// Spell with template and level data for fight AI.
/// Mirrors Bubble.D3.Bot's SpellWrapper.
/// </summary>
public sealed class SpellWrapper
{
    public int SpellId { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public int ApCost { get; set; }
    public int MinRange { get; set; }
    public int MaxRange { get; set; }
    public bool NeedLos { get; set; }
    public bool NeedFreeCell { get; set; }
    public bool CastInLine { get; set; }
    public bool CastInDiagonal { get; set; }
    public int CooldownDuration { get; set; }
    public int MaxCastPerTurn { get; set; }
    public int MaxCastPerTarget { get; set; }

    // Runtime state
    public int CooldownRemaining { get; set; }
    public int CastsThisTurn { get; set; }
    public Dictionary<long, int> CastsPerTarget { get; set; } = [];

    public bool CanCast(int ap) =>
        ap >= ApCost && CooldownRemaining <= 0 && CastsThisTurn < MaxCastPerTurn;

    public bool CanCastOnTarget(long targetId) =>
        MaxCastPerTarget <= 0 || (CastsPerTarget.GetValueOrDefault(targetId) < MaxCastPerTarget);

    public void OnNewTurn()
    {
        CastsThisTurn = 0;
        CastsPerTarget.Clear();
        if (CooldownRemaining > 0) CooldownRemaining--;
    }
}
