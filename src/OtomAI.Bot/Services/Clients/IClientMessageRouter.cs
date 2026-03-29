using OtomAI.Protocol.Dispatch;

namespace OtomAI.Bot.Services.Clients;

/// <summary>
/// Interface for message routing. Mirrors Bubble.D3.Bot's IClientMessageRouter.
/// </summary>
public interface IClientMessageRouter
{
    void OnMessageReceived(IProtoMessage message, string typeFullName);
}
