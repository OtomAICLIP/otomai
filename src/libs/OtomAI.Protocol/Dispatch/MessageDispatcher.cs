using ProtoBuf;
using Serilog;

namespace OtomAI.Protocol.Dispatch;

/// <summary>
/// Routes incoming GameMessages to registered handlers by TypeUrl.
/// Similar to Bubble.D3.Bot's source-generated MessageDispatcher,
/// but uses a runtime dictionary instead of codegen.
/// </summary>
public sealed class MessageDispatcher
{
    private readonly Dictionary<string, Func<byte[], MessageContext, CancellationToken, Task>> _handlers = new();

    public void Register<T>(IMessageHandler<T> handler) where T : class, IProtoMessage, new()
    {
        var typeUrl = T.TypeUrl;
        _handlers[typeUrl] = async (bytes, ctx, ct) =>
        {
            var msg = Serializer.Deserialize<T>((ReadOnlySpan<byte>)bytes);
            await handler.HandleAsync(msg, ctx, ct);
        };
        Log.Debug("Registered handler for {TypeUrl}", typeUrl);
    }

    public async Task DispatchAsync(GameMessage message, CancellationToken ct = default)
    {
        ProtobufAny? content;
        int uid;
        bool isEvent = false, isRequest = false, isResponse = false;

        if (message.Event is not null)
        {
            content = message.Event.Content;
            uid = message.Event.Uid;
            isEvent = true;
        }
        else if (message.Request is not null)
        {
            content = message.Request.Content;
            uid = message.Request.Uid;
            isRequest = true;
        }
        else if (message.Response is not null)
        {
            content = message.Response.Content;
            uid = message.Response.Uid;
            isResponse = true;
        }
        else
        {
            Log.Warning("Received empty GameMessage");
            return;
        }

        if (content is null)
        {
            Log.Warning("GameMessage has null content (uid={Uid})", uid);
            return;
        }

        var shortCode = content.ShortCode;

        if (_handlers.TryGetValue(shortCode, out var handler))
        {
            var ctx = new MessageContext
            {
                Uid = uid,
                TypeUrl = content.TypeUrl,
                IsEvent = isEvent,
                IsRequest = isRequest,
                IsResponse = isResponse,
            };
            await handler(content.Value, ctx, ct);
        }
        else
        {
            Log.Verbose("No handler for TypeUrl {TypeUrl}", content.TypeUrl);
        }
    }
}
