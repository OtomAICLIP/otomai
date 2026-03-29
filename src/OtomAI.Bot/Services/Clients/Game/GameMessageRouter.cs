using OtomAI.Protocol;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Routes game messages through the handler chain.
/// Mirrors Bubble.D3.Bot's GameMessageRouter: ordered list of IGameMessageHandler,
/// first match (TryHandle returns true) stops propagation.
/// </summary>
public sealed class GameMessageRouter
{
    private readonly IGameMessageHandler[] _handlers;

    public GameMessageRouter(params IGameMessageHandler[] handlers)
    {
        _handlers = handlers;
    }

    public async Task RouteAsync(GameMessage message)
    {
        var content = message.Event?.Content ?? message.Request?.Content ?? message.Response?.Content;
        if (content is null) return;

        var typeUrl = content.ShortCode;

        foreach (var handler in _handlers)
        {
            try
            {
                if (handler.TryHandle(message, typeUrl))
                    return;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Handler {Handler} failed for {TypeUrl}",
                    handler.GetType().Name, typeUrl);
            }
        }

        Log.Verbose("Unhandled game message: {TypeUrl}", typeUrl);
        await Task.CompletedTask;
    }
}
