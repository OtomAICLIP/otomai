using Bubble.Shared.Protocol;

namespace BubbleBot.Cli.Services.Clients;

internal interface IClientMessageRouter
{
    void OnMessageReceived(IProtoMessage? message, string? typeFullName);
}
