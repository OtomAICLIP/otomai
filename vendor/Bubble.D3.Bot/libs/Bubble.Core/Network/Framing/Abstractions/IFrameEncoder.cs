using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Abstractions;

public interface IFrameEncoder : IAsyncDisposable, IDisposable
{
    long FramesWritten { get; }
    bool IsDisposed { get; }

    ValueTask WriteAsync(IAsyncEnumerable<Frame> frames, CancellationToken token = default);

    ValueTask WriteAsync(IEnumerable<Frame> frames, CancellationToken token = default);

    ValueTask WriteAsync(in Frame frame, CancellationToken token = default);
}

public interface IFrameEncoder<TMetadata> : IFrameEncoder where TMetadata : class, IFrameMetadata
{
    ValueTask WriteAsync(IAsyncEnumerable<Frame<TMetadata>> frames, CancellationToken token = default);

    ValueTask WriteAsync(IEnumerable<Frame<TMetadata>> frames, CancellationToken token = default);

    ValueTask WriteAsync(in Frame<TMetadata> frame, CancellationToken token = default);
}