namespace OtomAI.Bot.Fight;

/// <summary>
/// Fight simulation for AI decision-making.
/// Mirrors Bubble.D3.Bot's FightSimulation (IMapInfo wrapper for damage calc).
/// </summary>
public sealed class FightSimulation
{
    private readonly FightInfo _fight;

    public FightSimulation(FightInfo fight)
    {
        _fight = fight;
    }

    public int EstimateDamage(SpellWrapper spell, FightActor target)
    {
        // TODO: Full damage calculation considering:
        // - Spell base damage
        // - Caster stats (strength, intelligence, etc.)
        // - Target resistances
        // - Critical hit chance
        // - Distance/range modifiers
        return 0;
    }

    public AiCellResult EvaluateCell(int cellId, FightActor actor)
    {
        // TODO: Score a cell position for AI decision-making
        // Consider: danger from enemies, range to targets, LoS
        return new AiCellResult { CellId = cellId, Score = 0 };
    }

    public FightActor? FindBestTarget(SpellWrapper spell)
    {
        // TODO: Find the optimal target for a spell
        // Consider: damage output, priority (low HP targets), accessibility
        return null;
    }
}

public sealed class AiCellResult
{
    public int CellId { get; set; }
    public double Score { get; set; }
}

public sealed class AiEnvironment
{
    public FightInfo Fight { get; set; } = null!;
    public FightActor Self { get; set; } = null!;
    public List<SpellWrapper> AvailableSpells { get; set; } = [];
}
