using OtomAI.Bot.Models;
using OtomAI.Core.Collections;
using Serilog;

namespace OtomAI.Bot.Client;

/// <summary>
/// Singleton managing all bot instances.
/// Mirrors Bubble.D3.Bot's BotManager: loads accounts, starts/stops bots,
/// monitors health, handles reconnection.
/// </summary>
public sealed class BotManager
{
    private static readonly Lazy<BotManager> _instance = new(() => new BotManager());
    public static BotManager Instance => _instance.Value;

    private readonly ConcurrentList<BotGameClient> _bots = new();
    private readonly Lock _lock = new();

    public IReadOnlyList<BotGameClient> Bots => _bots.ToList();
    public int ActiveCount => _bots.Count;

    private BotManager() { }

    public async Task<BotGameClient?> StartBotAsync(AccountSettings account, CancellationToken ct = default)
    {
        Log.Information("Starting bot for {Email} on server {Server}...", account.Email, account.ServerId);

        try
        {
            // Phase 1: Login server
            var loginClient = new BotClient(account.Email);
            await loginClient.ConnectAsync(account.Password, account.LoginHost, account.LoginPort, ct);

            // Wait for server selection (in real implementation, this would be event-driven)
            if (loginClient.SessionToken is null || loginClient.GameServerHost is null)
            {
                Log.Warning("No session token received for {Email}", account.Email);
                return null;
            }

            // Phase 2: Game server
            var gameClient = new BotGameClient(loginClient, loginClient.SessionToken, loginClient.SelectedServerId);
            await gameClient.ConnectAsync(loginClient.GameServerHost, loginClient.GameServerPort, ct);

            _bots.Add(gameClient);
            Log.Information("Bot started for {Email}", account.Email);
            return gameClient;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start bot for {Email}", account.Email);
            return null;
        }
    }

    public async Task StopBotAsync(BotGameClient bot)
    {
        _bots.Remove(bot);
        await bot.DisposeAsync();
        Log.Information("Bot stopped for {Email}", bot.LoginClient.AccountEmail);
    }

    public async Task StopAllAsync()
    {
        var bots = _bots.ToList();
        foreach (var bot in bots)
            await StopBotAsync(bot);
    }

    public BotGameClient? GetBot(string email) =>
        _bots.FirstOrDefault(b => b.LoginClient.AccountEmail == email);

    public async Task MonitorLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);

            var bots = _bots.ToList();
            foreach (var bot in bots)
            {
                var elapsed = DateTime.UtcNow - bot.State.LastActivityTime;
                if (elapsed > TimeSpan.FromMinutes(5))
                {
                    Log.Warning("Bot {Email} inactive for {Minutes}m",
                        bot.LoginClient.AccountEmail, elapsed.TotalMinutes);
                }
            }
        }
    }
}
