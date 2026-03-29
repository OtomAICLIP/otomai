using Bubble.Core.Network.Framing.Abstractions;
using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Serialization;

public interface IMessageReader
{
    bool TryDecode<T>(in Frame frame, T message);
}

public interface IMessageReader<TMeta> : IMessageReader where TMeta : class, IFrameMetadata
{
    bool TryDecode<T>(in Frame<TMeta> frame, T message);
}