using Bubble.Shared.Protocol;

namespace BubbleBot.Cli.Services.Clients;

internal sealed class MutableClientMessageRouter : IClientMessageRouter
{
    public IClientMessageRouter? Inner { get; set; }

    public void OnMessageReceived(IProtoMessage? message, string? typeFullName)
    {
        Inner?.OnMessageReceived(message, typeFullName);
    }
}
