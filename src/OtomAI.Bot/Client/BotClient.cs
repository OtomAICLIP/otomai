using OtomAI.Core.Network;
using OtomAI.Protocol;
using OtomAI.Protocol.Auth;
using OtomAI.Protocol.Dispatch;
using OtomAI.Protocol.Messages;
using ProtoBuf;
using Serilog;

namespace OtomAI.Bot.Client;

/// <summary>
/// Single bot instance managing auth + game connection.
/// Follows Bubble.D3.Bot's BotClient pattern: auth server -> get ticket -> game server.
/// </summary>
public sealed class BotClient : IAsyncDisposable
{
    private readonly GameConnection _connection = new();
    private readonly MessageDispatcher _dispatcher = new();
    private readonly AnkamaAuth _auth = new();
    private Timer? _keepAlive;
    private int _nextUid;

    public string AccountEmail { get; }
    public BotState State { get; private set; } = BotState.Disconnected;

    public BotClient(string accountEmail)
    {
        AccountEmail = accountEmail;
        _connection.OnMessage += OnRawMessageAsync;
        _connection.OnDisconnected += ex =>
        {
            State = BotState.Disconnected;
            Log.Error(ex, "Bot {Email} disconnected", AccountEmail);
        };
    }

    public void RegisterHandler<T>(IMessageHandler<T> handler) where T : class, IProtoMessage, new()
    {
        _dispatcher.Register(handler);
    }

    public async Task ConnectAsync(string password, string gameHost, int gamePort, CancellationToken ct = default)
    {
        State = BotState.Authenticating;
        Log.Information("Authenticating {Email}...", AccountEmail);

        var tokens = await _auth.LoginAsync(AccountEmail, password, ct);

        State = BotState.Connecting;
        Log.Information("Connecting to game server {Host}:{Port}...", gameHost, gamePort);

        await _connection.ConnectAsync(gameHost, gamePort, ct);

        // Send identification with OAuth token
        await SendRequestAsync(new IdentificationRequest
        {
            TicketKey = tokens.AccessToken,
            Language = "en",
        }, ct);

        State = BotState.Connected;

        // Start keep-alive ping every 30 seconds
        _keepAlive = new Timer(async _ =>
        {
            try
            {
                await SendRequestAsync(new PingRequest { Quiet = true }, ct);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Keep-alive failed for {Email}", AccountEmail);
            }
        }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    public async Task SendRequestAsync<T>(T message, CancellationToken ct = default) where T : class, IProtoMessage
    {
        using var ms = new MemoryStream();
        Serializer.Serialize(ms, message);

        var gameMsg = new GameMessage
        {
            Request = new GameRequest
            {
                Uid = Interlocked.Increment(ref _nextUid),
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
            await _dispatcher.DispatchAsync(msg);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to deserialize message ({Bytes} bytes)", data.Length);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _keepAlive?.Dispose();
        await _connection.DisposeAsync();
    }
}

public enum BotState
{
    Disconnected,
    Authenticating,
    Connecting,
    Connected,
    InGame,
}
