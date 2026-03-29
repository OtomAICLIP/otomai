using System.Buffers;
using Bubble.Core.Network.Framing.Serialization;
using ProtoBuf;

namespace Bubble.Core.Network.Framing.Protobuf;

public sealed class ProtobufMessageReader : MessageReader<ProtobufMetadata>
{
    protected override bool TryDeserialize<T>(in ReadOnlySequence<byte> payload, T message)
    {
        try
        {
            Serializer.Deserialize(payload, message);
            return true;
        }
        catch
        {
            return false;
        }
    }
}