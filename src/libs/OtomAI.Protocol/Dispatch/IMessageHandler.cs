namespace OtomAI.Protocol.Dispatch;

/// <summary>
/// Marker interface for protobuf game messages.
/// Each concrete message type provides its TypeUrl short code.
/// </summary>
public interface IProtoMessage
{
    static abstract string TypeUrl { get; }
}

/// <summary>
/// Handles a specific game message type.
/// </summary>
public interface IMessageHandler<T> where T : IProtoMessage
{
    Task HandleAsync(T message, MessageContext context, CancellationToken ct = default);
}

/// <summary>
/// Context passed to message handlers with connection metadata.
/// </summary>
public sealed class MessageContext
{
    public required int Uid { get; init; }
    public required string TypeUrl { get; init; }
    public required bool IsEvent { get; init; }
    public required bool IsRequest { get; init; }
    public required bool IsResponse { get; init; }
}
