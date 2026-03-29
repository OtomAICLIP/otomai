namespace BubbleBot.Cli.Services.Clients;

internal sealed class DeferredMessageRouter : IClientMessageRouter
{
    private IClientMessageRouter? _innerRouter;

    public void Attach(IClientMessageRouter innerRouter)
    {
        _innerRouter = innerRouter;
    }

    public void OnMessageReceived(Bubble.Shared.Protocol.IProtoMessage? message, string? typeFullName)
    {
        _innerRouter?.OnMessageReceived(message, typeFullName);
    }
}
