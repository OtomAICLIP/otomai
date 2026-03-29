using System.Buffers;
using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Serialization;

public abstract class MessageWriter : IMessageWriter
{
    private readonly IMetadataEncoder _encoder;

    protected MessageWriter(IMetadataEncoder encoder)
    {
        _encoder = encoder;
    }

    protected abstract IFrameMetadata GetFrameMetadataOf<T>(in T message);

    protected abstract void Serialize<T>(in T message, IBufferWriter<byte> writer);

    public void Encode<T>(in T message, IBufferWriter<byte> writer)
    {
        var metadata = GetFrameMetadataOf(in message);
        var metaLen = _encoder.GetLength(metadata);
        var span = writer.GetSpan(metaLen);

        _encoder.Write(ref span, metadata);

        writer.Advance(metaLen);

        // The payload is written first in order to handle potential differences between the
        // number of bytes written and the pre-calculated length
        if (metadata.Length > 0)
            Serialize(in message, writer);
    }
}

public abstract class MessageWriter<TMeta> : MessageWriter
    where TMeta : class, IFrameMetadata
{
    protected MessageWriter(IMetadataEncoder encoder) : base(encoder)
    {
    }

    protected override IFrameMetadata GetFrameMetadataOf<T>(in T message)
    {
        return GetMetadataOf(in message);
    }

    protected abstract TMeta GetMetadataOf<T>(in T message);
}