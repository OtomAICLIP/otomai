using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Abstractions;

public interface IFrameDecoder : IAsyncDisposable, IDisposable
{
    long FramesRead { get; }

    ValueTask<Frame> ReadFrameAsync(CancellationToken token = default);

    IAsyncEnumerable<Frame> ReadFramesAsync(CancellationToken token = default);
}

public interface IFrameDecoder<TMetadata> : IFrameDecoder
    where TMetadata : class, IFrameMetadata
{
    new ValueTask<Frame<TMetadata>> ReadFrameAsync(CancellationToken token = default);

    new IAsyncEnumerable<Frame<TMetadata>> ReadFramesAsync(CancellationToken token = default);
}