using Bubble.Core.Network.Framing.Abstractions;
using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing;

public interface IFrameMessageEncoder : IMessageEncoder, IFrameEncoder;

public interface IFrameMessageEncoder<TMeta> : IFrameMessageEncoder, IFrameEncoder<TMeta>
    where TMeta : class, IFrameMetadata;