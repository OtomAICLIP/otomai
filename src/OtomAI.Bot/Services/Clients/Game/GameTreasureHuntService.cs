using OtomAI.Bot.Client;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Treasure hunt state machine.
/// Mirrors Bubble.D3.Bot's GameTreasureHuntService.
/// </summary>
public sealed class GameTreasureHuntService : GameClientServiceBase
{
    public GameTreasureHuntService(BotGameClient client) : base(client) { }

    public void HandleTreasureHuntUpdate(int step, int totalSteps)
    {
        State.InTreasureHunt = true;
        State.TreasureHuntStep = step;
        Log.Information("{Name} treasure hunt step {Step}/{Total}", CharacterName, step, totalSteps);
    }

    public void HandleTreasureHuntFinished(bool success)
    {
        State.InTreasureHunt = false;
        State.TreasureHuntStep = 0;
        Log.Information("{Name} treasure hunt {Result}", CharacterName, success ? "completed" : "failed");
    }

    public async Task StartTreasureHuntAsync(CancellationToken ct = default)
    {
        Log.Information("{Name} starting treasure hunt", CharacterName);
        // TODO: Send treasure hunt start request
        await Task.CompletedTask;
    }

    public async Task ProcessCurrentStepAsync(CancellationToken ct = default)
    {
        // TODO: Resolve clue, navigate to target, dig/interact
        await Task.CompletedTask;
    }
}
