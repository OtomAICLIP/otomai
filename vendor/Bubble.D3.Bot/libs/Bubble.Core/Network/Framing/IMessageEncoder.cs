namespace Bubble.Core.Network.Framing;

public interface IMessageEncoder
{
    ReadOnlyMemory<byte> Encode<TMessage>(in TMessage message);
    ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken token = default);
    ValueTask WriteAsync<TMessage>(in TMessage message, CancellationToken token = default);
}