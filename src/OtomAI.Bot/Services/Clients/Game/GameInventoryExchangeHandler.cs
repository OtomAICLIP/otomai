using OtomAI.Bot.Client;
using OtomAI.Protocol;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Handles inventory, exchange, and bid house messages.
/// Mirrors Bubble.D3.Bot's GameInventoryExchangeHandler.
/// </summary>
public sealed class GameInventoryExchangeHandler : GameClientServiceBase, IGameMessageHandler
{
    public GameInventoryExchangeHandler(BotGameClient client) : base(client) { }

    public bool TryHandle(GameMessage message, string typeUrl)
    {
        // TODO: Implement handlers for:
        // - Inventory update
        // - Exchange requests/responses
        // - Bid house operations
        // - Item equip/unequip
        // - Kamas update

        switch (typeUrl)
        {
            default:
                return false;
        }
    }
}
