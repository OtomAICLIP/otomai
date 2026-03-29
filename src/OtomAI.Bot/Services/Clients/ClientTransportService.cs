using OtomAI.Bot.Client.Context;
using OtomAI.Core.Network;
using OtomAI.Protocol;
using OtomAI.Protocol.Dispatch;
using ProtoBuf;
using Serilog;

namespace OtomAI.Bot.Services.Clients;

/// <summary>
/// Core transport service: receives raw bytes, deserializes protobuf, dispatches.
/// Mirrors Bubble.D3.Bot's ClientTransportService.
/// </summary>
public sealed class ClientTransportService
{
    private readonly BotClientContextBase _context;
    private readonly GameConnection _connection;

    public ClientTransportService(BotClientContextBase context, GameConnection connection)
    {
        _context = context;
        _connection = connection;
    }

    public async Task SendAuthenticationTicketAsync(string ticket, CancellationToken ct = default)
    {
        Log.Debug("Sending authentication ticket");
        // In the full implementation, this sends the auth ticket message
        // For now, stubbed to match the reference architecture
        await Task.CompletedTask;
    }

    public async Task SendMessageAsync(GameMessage message, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        Serializer.Serialize(ms, message);
        await _connection.SendAsync(ms.ToArray(), ct);
    }

    public async Task SendRequestAsync<T>(T message, int uid, CancellationToken ct = default) where T : class, IProtoMessage
    {
        using var contentMs = new MemoryStream();
        Serializer.Serialize(contentMs, message);

        var gameMsg = new GameMessage
        {
            Request = new GameRequest
            {
                Uid = uid,
                Content = new ProtobufAny
                {
                    TypeUrl = $"type.ankama.com/{T.TypeUrl}",
                    Value = contentMs.ToArray(),
                },
            },
        };

        await SendMessageAsync(gameMsg, ct);
    }
}
