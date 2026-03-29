using System.Collections.Concurrent;
using Discord.Webhook;

namespace BubbleBot.Cli.Services.Clients;

internal sealed class BotGameClientContext : BotClientContextBase
{
    public BotGameClientContext(BotGameClient client,
                                BotClient     botController,
                                string        token,
                                BotSettings   settings)
        : base(client.BotId, client.Hwid, client.ServerId, client.ServerName)
    {
        Client = client;
        BotController = botController;
        Token = token;
        Settings = settings;
    }

    public BotGameClient Client { get; }
    public BotClient BotController { get; }
    public string Token { get; }
    public BotSettings Settings { get; }
    public GameRuntimeState State { get; } = new();
    public ConcurrentQueue<Func<Task>> Requests { get; } = new();
    public CancellationTokenSource MoveToCellRequestCts { get; set; } = new();
    public CancellationTokenSource ConnectionTimeoutCts { get; set; } = new();
    public List<DiscordWebhookClient?> FinalLogChannels { get; } = [];
}
