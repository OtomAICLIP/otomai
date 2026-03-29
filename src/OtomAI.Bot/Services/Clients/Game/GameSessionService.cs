using OtomAI.Bot.Client;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Connection lifecycle: init, keepalive, reconnection.
/// Mirrors Bubble.D3.Bot's GameSessionService.
/// </summary>
public sealed class GameSessionService : GameClientServiceBase
{
    private Timer? _keepAlive;

    public GameSessionService(BotGameClient client) : base(client) { }

    public void StartKeepAlive(TimeSpan interval)
    {
        _keepAlive = new Timer(_ =>
        {
            try
            {
                // TODO: Send ping request
                State.LastActivityTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Keep-alive failed for {Name}", CharacterName);
            }
        }, null, interval, interval);
    }

    public void StopKeepAlive()
    {
        _keepAlive?.Dispose();
        _keepAlive = null;
    }

    public async Task HandleReconnectionAsync(CancellationToken ct = default)
    {
        Log.Information("Attempting reconnection for {Name}...", CharacterName);
        // TODO: Implement reconnection logic
        await Task.CompletedTask;
    }
}
