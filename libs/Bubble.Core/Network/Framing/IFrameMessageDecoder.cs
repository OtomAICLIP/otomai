using Bubble.Core.Network.Framing.Abstractions;
using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing;

public interface IFrameMessageDecoder : IMessageDecoder, IFrameDecoder;

public interface IFrameMessageDecoder<TMeta> : IFrameMessageDecoder, IFrameDecoder<TMeta>
    where TMeta : class, IFrameMetadata;