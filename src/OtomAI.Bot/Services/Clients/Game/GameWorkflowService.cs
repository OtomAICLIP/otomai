using OtomAI.Bot.Client;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Main bot logic: DoWork() state machine.
/// Mirrors Bubble.D3.Bot's GameWorkflowService.
/// </summary>
public sealed class GameWorkflowService : GameClientServiceBase
{
    public GameWorkflowService(BotGameClient client) : base(client) { }

    /// <summary>
    /// Main bot work loop. Called after character is loaded and map is ready.
    /// Mirrors Bubble.D3.Bot's DoWork() -> DoWorkInternal() state machine:
    /// 1. DoRestat() - boost stats
    /// 2. DoAutoOpen() - use items
    /// 3. Leave HavenBag if needed
    /// 4. Trajet mode / ZaapMode / Koli mode / TreasureHunt mode
    /// </summary>
    public async Task DoWorkAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await DoWorkInternalAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "DoWork error for {Name}", CharacterName);
                await Task.Delay(5000, ct);
            }
        }
    }

    private async Task DoWorkInternalAsync(CancellationToken ct)
    {
        if (InFight)
        {
            await Task.Delay(1000, ct);
            return;
        }

        // TODO: Implement work modes based on account settings
        // For now, basic trajet (route) following
        if (State.CurrentTrajet is not null)
        {
            await DoWorkTrajetAsync(ct);
            return;
        }

        if (InTreasureHunt)
        {
            await DoWorkTreasureHuntAsync(ct);
            return;
        }

        // Default: idle
        await Task.Delay(5000, ct);
    }

    private async Task DoWorkTrajetAsync(CancellationToken ct)
    {
        Log.Debug("Following trajet step {Step} for {Name}", State.TrajetStepIndex, CharacterName);
        // TODO: Follow route steps, fight monsters, party sync
        await Task.Delay(2000, ct);
    }

    private async Task DoWorkTreasureHuntAsync(CancellationToken ct)
    {
        Log.Debug("Processing treasure hunt step {Step} for {Name}", State.TreasureHuntStep, CharacterName);
        // TODO: Delegate to TreasureHuntService
        await Task.Delay(2000, ct);
    }
}
