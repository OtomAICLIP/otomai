using OtomAI.Bot.Client.Context;
using OtomAI.Bot.Services.Clients;
using OtomAI.Bot.Services.Clients.Game;
using OtomAI.Core.Network;
using OtomAI.Protocol;
using OtomAI.Protocol.Dispatch;
using ProtoBuf;
using Serilog;

namespace OtomAI.Bot.Client;

/// <summary>
/// Game server TCP client. Phase 2 of the connection model.
/// Mirrors Bubble.D3.Bot's BotGameClient: facade over all game services.
/// Services are manually constructed (no IoC container), matching the reference architecture.
/// </summary>
public sealed class BotGameClient : IAsyncDisposable
{
    private readonly GameConnection _connection = new();

    // Context
    public BotGameClientContext Context { get; }
    public BotClient LoginClient { get; }

    // Services (manually constructed per Bubble.D3.Bot pattern)
    public ClientTransportService Transport { get; }
    public ClientVerificationService Verification { get; }
    public GameMessageRouter MessageRouter { get; }
    public GameSessionService Session { get; }
    public GameWorkflowService Workflow { get; }
    public GameMapLifecycleService MapLifecycle { get; }
    public GameNavigationService Navigation { get; }
    public GameTravelService Travel { get; }
    public GameTreasureHuntService TreasureHunt { get; }
    public GameNotificationService Notification { get; }

    // Handler chain
    public GameSessionSystemHandler SessionSystemHandler { get; }
    public GameFightHandler FightHandler { get; }
    public GameInventoryExchangeHandler InventoryExchangeHandler { get; }
    public GameWorldTreasureHandler WorldTreasureHandler { get; }
    public GamePartyGuildChatHandler PartyGuildChatHandler { get; }
    public GameArenaKoliHandler ArenaKoliHandler { get; }

    // State delegates (from GameClientServiceBase pattern)
    public GameRuntimeState State => Context.RuntimeState;

    public BotGameClient(BotClient loginClient, string sessionToken, int serverId)
    {
        LoginClient = loginClient;

        Context = new BotGameClientContext
        {
            GameClient = this,
            SessionToken = sessionToken,
            ServerId = serverId,
        };

        // Build service graph (same order as Bubble.D3.Bot's BotGameClient constructor)
        Transport = new ClientTransportService(Context, _connection);
        Verification = new ClientVerificationService(Context);
        Notification = new GameNotificationService(this);
        Session = new GameSessionService(this);
        Travel = new GameTravelService(this);
        Navigation = new GameNavigationService(this);
        MapLifecycle = new GameMapLifecycleService(this);
        TreasureHunt = new GameTreasureHuntService(this);
        Workflow = new GameWorkflowService(this);

        // Build handler chain (order matters - first match wins)
        SessionSystemHandler = new GameSessionSystemHandler(this);
        FightHandler = new GameFightHandler(this);
        InventoryExchangeHandler = new GameInventoryExchangeHandler(this);
        WorldTreasureHandler = new GameWorldTreasureHandler(this);
        PartyGuildChatHandler = new GamePartyGuildChatHandler(this);
        ArenaKoliHandler = new GameArenaKoliHandler(this);

        MessageRouter = new GameMessageRouter(
            SessionSystemHandler,
            InventoryExchangeHandler,
            WorldTreasureHandler,
            FightHandler,
            PartyGuildChatHandler,
            ArenaKoliHandler
        );

        _connection.OnMessage += OnRawMessageAsync;
        _connection.OnDisconnected += ex =>
            Log.Error(ex, "Game client disconnected for {Email}", loginClient.AccountEmail);
    }

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        Log.Information("Connecting to game server {Host}:{Port}...", host, port);
        await _connection.ConnectAsync(host, port, ct);

        // Send authentication ticket
        await Transport.SendAuthenticationTicketAsync(Context.SessionToken, ct);
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
            Log.Error(ex, "Failed to process game message ({Bytes} bytes)", data.Length);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Context.Cts.Cancel();
        Context.WorkCts?.Cancel();
        await _connection.DisposeAsync();
    }
}
