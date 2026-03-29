using OtomAI.Bot.Client;
using OtomAI.Protocol;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Handles map events, movement, treasure hunts, teleport.
/// Mirrors Bubble.D3.Bot's GameWorldTreasureHandler.
/// </summary>
public sealed class GameWorldTreasureHandler : GameClientServiceBase, IGameMessageHandler
{
    public GameWorldTreasureHandler(BotGameClient client) : base(client) { }

    public bool TryHandle(GameMessage message, string typeUrl)
    {
        // TODO: Implement handlers for:
        // - Map complement (new map data)
        // - Movement confirmed/rejected
        // - Teleport
        // - Interactive element use
        // - Treasure hunt update/finish
        // - Entity appear/disappear on map

        switch (typeUrl)
        {
            default:
                return false;
        }
    }
}
