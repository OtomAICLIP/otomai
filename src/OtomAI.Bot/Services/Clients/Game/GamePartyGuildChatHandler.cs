using OtomAI.Bot.Client;
using OtomAI.Protocol;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Handles party management, guild, and chat messages.
/// Mirrors Bubble.D3.Bot's GamePartyGuildChatHandler.
/// </summary>
public sealed class GamePartyGuildChatHandler : GameClientServiceBase, IGameMessageHandler
{
    public GamePartyGuildChatHandler(BotGameClient client) : base(client) { }

    public bool TryHandle(GameMessage message, string typeUrl)
    {
        // TODO: Implement handlers for:
        // - Party invite/join/leave/kick
        // - Party member update
        // - Guild invite/info
        // - Chat messages (general, party, guild, whisper)

        switch (typeUrl)
        {
            default:
                return false;
        }
    }
}
