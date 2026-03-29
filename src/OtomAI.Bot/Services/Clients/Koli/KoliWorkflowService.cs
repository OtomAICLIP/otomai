using OtomAI.Bot.Client;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Koli;

/// <summary>
/// Koli fight workflow. Mirrors Bubble.D3.Bot's KoliWorkflowService.
/// </summary>
public sealed class KoliWorkflowService : KoliClientServiceBase
{
    public KoliWorkflowService(BotKoliClient client) : base(client) { }

    public async Task DoWorkAsync(CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            if (State.InFight && State.IsMyTurn)
            {
                await PlayTurnAsync(ct);
            }
            await Task.Delay(500, ct);
        }
    }

    private async Task PlayTurnAsync(CancellationToken ct)
    {
        Log.Debug("Koli fight turn {Turn}", State.FightTurn);
        // TODO: Use fight AI to play turn
        await Task.CompletedTask;
    }
}
