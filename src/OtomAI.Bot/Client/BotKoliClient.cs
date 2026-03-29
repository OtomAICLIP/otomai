using OtomAI.Bot.Client.Context;
using OtomAI.Bot.Services.Clients.Koli;
using OtomAI.Core.Network;
using OtomAI.Protocol;
using OtomAI.Protocol.Dispatch;
using ProtoBuf;
using Serilog;

namespace OtomAI.Bot.Client;

/// <summary>
/// Kolosseum fight server TCP client.
/// Mirrors Bubble.D3.Bot's BotKoliClient: created when ArenaSwitchToFightServerEvent
/// is received from the game server, connecting to a separate fight server.
/// </summary>
public sealed class BotKoliClient : IAsyncDisposable
{
    private readonly GameConnection _connection = new();

    public BotKoliClientContext Context { get; }
    public BotGameClient GameClient { get; }
    public KoliMessageRouter MessageRouter { get; }
    public KoliWorkflowService Workflow { get; }

    public BotKoliClient(BotGameClient gameClient, string sessionToken)
    {
        GameClient = gameClient;

        Context = new BotKoliClientContext
        {
            KoliClient = this,
            SessionToken = sessionToken,
        };

        Workflow = new KoliWorkflowService(this);
        MessageRouter = new KoliMessageRouter(this);

        _connection.OnMessage += OnRawMessageAsync;
        _connection.OnDisconnected += ex =>
            Log.Error(ex, "Koli client disconnected");
    }

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        Log.Information("Connecting to Koli server {Host}:{Port}...", host, port);
        await _connection.ConnectAsync(host, port, ct);
    }

    public async Task SendRequestAsync<T>(T message, CancellationToken ct = default) where T : class, IProtoMessage
    {
        using var ms = new MemoryStream();
        Serializer.Serialize(ms, message);

        var gameMsg = new GameMessage
        {
            Request = new GameRequest
            {
                Uid = Context.NextUid(),
                Content = new ProtobufAny
                {
                    TypeUrl = $"type.ankama.com/{T.TypeUrl}",
                    Value = ms.ToArray(),
                },
            },
        };

        using var frameMs = new MemoryStream();
        Serializer.Serialize(frameMs, gameMsg);
        await _connection.SendAsync(frameMs.ToArray(), ct);
    }

    private async Task OnRawMessageAsync(ReadOnlyMemory<byte> data)
    {
        try
        {
            var msg = Serializer.Deserialize<GameMessage>(data.Span);
            await MessageRouter.RouteAsync(msg);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process koli message ({Bytes} bytes)", data.Length);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Context.Cts.Cancel();
        await _connection.DisposeAsync();
    }
}
