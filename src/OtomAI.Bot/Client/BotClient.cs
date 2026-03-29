using OtomAI.Core.Network;
using OtomAI.Protocol;
using OtomAI.Protocol.Auth;
using OtomAI.Protocol.Dispatch;
using OtomAI.Protocol.Messages;
using ProtoBuf;
using Serilog;

namespace OtomAI.Bot.Client;

/// <summary>
/// Login (connection) server TCP client.
/// Phase 1 of the two-phase connection model from Bubble.D3.Bot:
/// 1. BotClient connects to login server, authenticates, selects server, gets token
/// 2. BotGameClient connects to game server with the token
///
/// The old monolithic BotClient has been split per Bubble.D3.Bot's architecture.
/// </summary>
public sealed class BotClient : IAsyncDisposable
{
    private readonly GameConnection _connection = new();
    private readonly MessageDispatcher _dispatcher = new();
    private readonly AnkamaAuth _auth = new();
    private int _nextUid;

    public string AccountEmail { get; }
    public BotState State { get; private set; } = BotState.Disconnected;

    // Populated after successful login + server selection
    public string? SessionToken { get; private set; }
    public string? GameServerHost { get; private set; }
    public int GameServerPort { get; private set; }
    public int SelectedServerId { get; private set; }

    public BotClient(string accountEmail)
    {
        AccountEmail = accountEmail;
        _connection.OnMessage += OnRawMessageAsync;
        _connection.OnDisconnected += ex =>
        {
            State = BotState.Disconnected;
            Log.Error(ex, "Login client {Email} disconnected", AccountEmail);
        };
    }

    public void RegisterHandler<T>(IMessageHandler<T> handler) where T : class, IProtoMessage, new()
    {
        _dispatcher.Register(handler);
    }

    /// <summary>
    /// Authenticate with Ankama, connect to login server, and get a game session token.
    /// After this completes, read SessionToken/GameServerHost/GameServerPort to create a BotGameClient.
    /// </summary>
    public async Task ConnectAsync(string password, string loginHost, int loginPort, CancellationToken ct = default)
    {
        State = BotState.Authenticating;
        Log.Information("Authenticating {Email}...", AccountEmail);

        var tokens = await _auth.LoginAsync(AccountEmail, password, ct);

        State = BotState.Connecting;
        Log.Information("Connecting to login server {Host}:{Port}...", loginHost, loginPort);

        await _connection.ConnectAsync(loginHost, loginPort, ct);

        await SendRequestAsync(new IdentificationRequest
        {
            TicketKey = tokens.AccessToken,
            Language = "en",
        }, ct);

        State = BotState.Connected;
    }

    /// <summary>
    /// Called when server selection response arrives with game server address.
    /// </summary>
    public void SetGameServerInfo(string host, int port, string sessionToken, int serverId)
    {
        GameServerHost = host;
        GameServerPort = port;
        SessionToken = sessionToken;
        SelectedServerId = serverId;
        Log.Information("Game server selected: {Host}:{Port} (server {Id})", host, port, serverId);
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
            Log.Error(ex, "Failed to deserialize login message ({Bytes} bytes)", data.Length);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}

public enum BotState
{
    Disconnected,
    Authenticating,
    Connecting,
    Connected,
    SelectingServer,
    InGame,
}
