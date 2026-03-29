using OtomAI.Bot.Client;
using OtomAI.Protocol;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Koli;

/// <summary>
/// Routes Koli server messages. Mirrors Bubble.D3.Bot's KoliMessageRouter.
/// </summary>
public sealed class KoliMessageRouter
{
    private readonly BotKoliClient _client;

    public KoliMessageRouter(BotKoliClient client)
    {
        _client = client;
    }

    public async Task RouteAsync(GameMessage message)
    {
        var content = message.Event?.Content ?? message.Request?.Content ?? message.Response?.Content;
        if (content is null) return;

        var typeUrl = content.ShortCode;
        Log.Verbose("Koli message received: {TypeUrl}", typeUrl);

        // TODO: Route to Koli-specific handlers as protocol is decoded
        await Task.CompletedTask;
    }
}
