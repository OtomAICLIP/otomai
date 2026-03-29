using OtomAI.Protocol;
using OtomAI.Protocol.Dispatch;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Chain-of-responsibility game message handler.
/// Mirrors Bubble.D3.Bot's IGameMessageHandler:
/// TryHandle returns true to stop propagation, false to pass to next handler.
/// </summary>
public interface IGameMessageHandler
{
    bool TryHandle(GameMessage message, string typeUrl);
}
