using OtomAI.Bot.Client;
using OtomAI.Bot.Client.Context;

namespace OtomAI.Bot.Services.Clients.Koli;

/// <summary>
/// Base class for Koli services. Mirrors Bubble.D3.Bot's KoliClientServiceBase.
/// </summary>
public abstract class KoliClientServiceBase
{
    protected BotKoliClient Client { get; }
    protected BotKoliClientContext Context => Client.Context;
    protected KoliRuntimeState State => Context.RuntimeState;

    protected KoliClientServiceBase(BotKoliClient client)
    {
        Client = client;
    }
}
