using System.Buffers;

namespace Bubble.Core.Network.Framing.Serialization;

public interface IMessageWriter
{
    void Encode<T>(in T message, IBufferWriter<byte> writer);
}