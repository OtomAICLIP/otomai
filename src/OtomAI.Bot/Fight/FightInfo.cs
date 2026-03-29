using Serilog;

namespace OtomAI.Bot.Fight;

/// <summary>
/// Main fight controller. Mirrors Bubble.D3.Bot's FightInfo:
/// Manages AI steps, turn logic, placement, spell casting, movement.
/// </summary>
public sealed class FightInfo
{
    public long FightId { get; set; }
    public int Turn { get; set; }
    public bool IsPlacementPhase { get; set; }
    public bool IsMyTurn { get; set; }

    public FightActor? Self { get; set; }
    public List<FightActor> Allies { get; } = [];
    public List<FightActor> Enemies { get; } = [];
    public List<FightActor> AllActors => [.. Allies, .. Enemies];

    public int ActionPoints => Self?.ActionPoints ?? 0;
    public int MovementPoints => Self?.MovementPoints ?? 0;

    public async Task PlayTurnAsync(CancellationToken ct = default)
    {
        if (Self is null || !IsMyTurn) return;

        Log.Debug("Fight turn {Turn}: AP={AP}, MP={MP}", Turn, ActionPoints, MovementPoints);

        // AI step order (from Bubble.D3.Bot):
        // 1. Evaluate position (move away from danger if needed)
        // 2. Cast offensive spells on best target
        // 3. Use remaining MP to position for next turn
        // 4. End turn

        await StepMoveAsync(ct);
        await StepCastSpellsAsync(ct);
        await StepRepositionAsync(ct);
    }

    private async Task StepMoveAsync(CancellationToken ct)
    {
        // TODO: Evaluate if we need to move (too close to enemies, out of range, etc.)
        await Task.CompletedTask;
    }

    private async Task StepCastSpellsAsync(CancellationToken ct)
    {
        // TODO: For each available spell, find best target and cast
        await Task.CompletedTask;
    }

    private async Task StepRepositionAsync(CancellationToken ct)
    {
        // TODO: Use remaining MP to get to a safe/optimal position
        await Task.CompletedTask;
    }

    public void HandleFightEnd(bool won)
    {
        Log.Information("Fight {Id} ended: {Result}", FightId, won ? "victory" : "defeat");
    }
}
