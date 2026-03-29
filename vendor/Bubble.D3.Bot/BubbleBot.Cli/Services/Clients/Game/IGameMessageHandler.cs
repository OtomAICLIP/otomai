using Bubble.Shared.Protocol;

namespace BubbleBot.Cli.Services.Clients.Game;

internal interface IGameMessageHandler
{
    bool TryHandle(IProtoMessage message);
}
