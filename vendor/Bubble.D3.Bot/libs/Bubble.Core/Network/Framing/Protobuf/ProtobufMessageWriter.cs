using System.Buffers;
using System.Runtime.Serialization;
using Bubble.Core.Network.Framing.Abstractions.Metadata;
using Bubble.Core.Network.Framing.Serialization;
using ProtoBuf;

namespace Bubble.Core.Network.Framing.Protobuf;

public sealed class ProtobufMessageWriter : MessageWriter<ProtobufMetadata>
{
    public ProtobufMessageWriter(IMetadataEncoder encoder) : base(encoder)
    {
    }

    protected override ProtobufMetadata GetMetadataOf<T>(in T message)
    {
        return new ProtobufMetadata((int)Serializer.Measure(message).Length);
    }

    protected override void Serialize<T>(in T message, IBufferWriter<byte> writer)
    {
        try
        {
            Serializer.Serialize(writer, message);
        }
        catch (Exception e)
        {
            throw new SerializationException("Failed to serialize message.", e);
        }
    }
}