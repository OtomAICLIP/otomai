using System.Buffers;
using Bubble.Core.Network.Framing.Abstractions;
using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Serialization;

public abstract class MessageReader : IMessageReader
{
    protected abstract bool TryDeserialize<T>(in ReadOnlySequence<byte> payload, T message);

    public bool TryDecode<T>(in Frame frame, T message)
    {
        if (frame.IsPayloadEmpty())
            return true;

        var payload = frame.Payload;

        return TryDeserialize(in payload, message);
    }
}

public abstract class MessageReader<TMeta> : MessageReader, IMessageReader<TMeta>
    where TMeta : class, IFrameMetadata
{
    public bool TryDecode<T>(in Frame<TMeta> frame, T message)
    {
        if (frame.IsPayloadEmpty())
            return true;

        var payload = frame.Payload;

        return TryDeserialize(in payload, message);
    }
}