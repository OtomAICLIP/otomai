using OtomAI.Bot.Client;
using OtomAI.Protocol;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Handles fight lifecycle messages: placement, turns, spells, movement, end.
/// Mirrors Bubble.D3.Bot's GameFightHandler.
/// </summary>
public sealed class GameFightHandler : GameClientServiceBase, IGameMessageHandler
{
    public GameFightHandler(BotGameClient client) : base(client) { }

    public bool TryHandle(GameMessage message, string typeUrl)
    {
        // TODO: Implement handlers for fight messages as protocol is decoded
        // Known patterns from Bubble.D3.Bot:
        // - FightStarting (placement phase)
        // - FightTurnStart / FightTurnEnd
        // - FightResult (win/lose/draw)
        // - Spell cast acknowledgment
        // - Entity movement in fight
        // - Summon placement

        switch (typeUrl)
        {
            default:
                return false;
        }
    }
}
