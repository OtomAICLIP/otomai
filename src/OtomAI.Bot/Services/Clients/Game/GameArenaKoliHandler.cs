using OtomAI.Bot.Client;
using OtomAI.Protocol;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Handles arena registration, fight proposals, server switch.
/// Mirrors Bubble.D3.Bot's GameArenaKoliHandler.
/// </summary>
public sealed class GameArenaKoliHandler : GameClientServiceBase, IGameMessageHandler
{
    public GameArenaKoliHandler(BotGameClient client) : base(client) { }

    public bool TryHandle(GameMessage message, string typeUrl)
    {
        // TODO: Implement handlers for:
        // - Arena registration response
        // - Arena fight proposal
        // - ArenaSwitchToFightServerEvent (triggers BotKoliClient creation)
        // - Arena season info

        switch (typeUrl)
        {
            default:
                return false;
        }
    }
}
