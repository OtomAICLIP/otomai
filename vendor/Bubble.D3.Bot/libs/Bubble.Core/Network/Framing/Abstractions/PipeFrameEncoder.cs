using System.IO.Pipelines;
using Bubble.Core.Network.Framing.Abstractions.Extensions;
using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Abstractions;

public class PipeFrameEncoder : IFrameEncoder
{
    protected readonly IMetadataEncoder _encoder;

    private long _framesWritten;
    protected PipeWriter? _pipe;
    protected SemaphoreSlim? _singleWriter;

    public long FramesWritten =>
        Interlocked.Read(ref _framesWritten);

    public bool IsDisposed { get; private set; }

    protected PipeFrameEncoder(PipeWriter pipe, IMetadataEncoder encoder, SemaphoreSlim? singleWriter = default)
    {
        _singleWriter = singleWriter;
        _encoder = encoder;
        _pipe = pipe;
    }

    protected PipeFrameEncoder(Stream stream, IMetadataEncoder encoder, SemaphoreSlim? singleWriter = default)
    {
        _singleWriter = singleWriter;
        _encoder = encoder;
        _pipe = PipeWriter.Create(stream);
    }

    protected void Release(int framesWritten = 1)
    {
        // If the access to the pipe is already synchronized, add or increment using Interlocked class
        if (_singleWriter is not null)
            _framesWritten += framesWritten;
        else if (framesWritten is 1)
            Interlocked.Increment(ref _framesWritten);
        else
            Interlocked.Add(ref _framesWritten, framesWritten);

        _singleWriter?.Release();
    }

    protected bool TryWaitForSingleWriter(CancellationToken token = default)
    {
        return _singleWriter is null || _singleWriter.Wait(0, token);
    }

    protected Task WaitForSingleWriterAsync(CancellationToken token = default)
    {
        return _singleWriter is not null ? _singleWriter.WaitAsync(token) : Task.CompletedTask;
    }

    public ValueTask WriteAsync(IAsyncEnumerable<Frame> frames, CancellationToken token = default)
    {
        var writer = _pipe ?? throw new ObjectDisposedException(nameof(PipeFrameEncoder));

        // try to get the conch; if not, switch to async
        return TryWaitForSingleWriter(token) ? SendAll() : SendAllSlow();

        async ValueTask SendAll()
        {
            var framesWritten = 0;

            try
            {
                await foreach (var frame in frames.WithCancellation(token))
                {
                    var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);

                    var flushResult = writeAsync.IsCompletedSuccessfully
                        ? writeAsync.Result
                        : await writeAsync.ConfigureAwait(false);

                    framesWritten++;

                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                        break;
                }
            }
            finally
            {
                Release(framesWritten);
            }
        }

        async ValueTask SendAllSlow()
        {
            await WaitForSingleWriterAsync(token).ConfigureAwait(false);

            var framesWritten = 0;

            try
            {
                await foreach (var frame in frames.WithCancellation(token))
                {
                    var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);
                    var flushResult = writeAsync.IsCompletedSuccessfully
                        ? writeAsync.Result
                        : await writeAsync.ConfigureAwait(false);

                    framesWritten++;

                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                        break;
                }
            }
            finally
            {
                Release(framesWritten);
            }
        }
    }

    public ValueTask WriteAsync(IEnumerable<Frame> frames, CancellationToken token = default)
    {
        var writer = _pipe ?? throw new ObjectDisposedException(nameof(PipeFrameEncoder));

        // try to get the conch; if not, switch to async
        return TryWaitForSingleWriter(token) ? SendAll() : SendAllSlow();

        async ValueTask SendAll()
        {
            var framesWritten = 0;

            try
            {
                foreach (var frame in frames)
                {
                    var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);

                    var flushResult = writeAsync.IsCompletedSuccessfully
                        ? writeAsync.Result
                        : await writeAsync.ConfigureAwait(false);

                    framesWritten++;

                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                        break;
                }
            }
            finally
            {
                Release(framesWritten);
            }
        }

        async ValueTask SendAllSlow()
        {
            await WaitForSingleWriterAsync(token).ConfigureAwait(false);

            var framesWritten = 0;

            try
            {
                foreach (var frame in frames)
                {
                    var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);
                    var flushResult = writeAsync.IsCompletedSuccessfully
                        ? writeAsync.Result
                        : await writeAsync.ConfigureAwait(false);

                    framesWritten++;

                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                        break;
                }
            }
            finally
            {
                Release(framesWritten);
            }
        }
    }

    public ValueTask WriteAsync(in Frame frame, CancellationToken token = default)
    {
        var writer = _pipe ?? throw new ObjectDisposedException(nameof(PipeFrameEncoder));

        // try to get the conch; if not, switch to async
        if (!TryWaitForSingleWriter(token))
            return SendAsyncSlowPath(frame);

        var release = true;
        try
        {
            var write = writer.WriteFrameAsync(_encoder, in frame, token); // includes a flush

            if (write.IsCompletedSuccessfully)
                return default;

            release = false;
            return AwaitFlushAndRelease(write);
        }
        finally
        {
            if (release)
                Release();
        } // don't release here if we had to continue with an async path

        async ValueTask AwaitFlushAndRelease(ValueTask<FlushResult> flush)
        {
            try
            {
                await flush.ConfigureAwait(false);
            }
            finally
            {
                Release();
            }
        }

        async ValueTask SendAsyncSlowPath(Frame frm)
        {
            await WaitForSingleWriterAsync(token).ConfigureAwait(false);

            try
            {
                var writeAsync = writer.WriteFrameAsync(_encoder, in frm, token);

                if (!writeAsync.IsCompletedSuccessfully)
                    await writeAsync.ConfigureAwait(false);
            }
            finally
            {
                Release();
            }
        }
    }

    #pragma warning disable CA1816 // GC.SuppressFinalize is already called in Dispose
    public ValueTask DisposeAsync()
        #pragma warning restore CA1816 // GC.SuppressFinalize is already called in Dispose
    {
        Dispose();
        return default;
    }

    public virtual void Dispose()
    {
        IsDisposed = true;

        var semaphore = Interlocked.Exchange(ref _singleWriter, null);
        var pipe = Interlocked.Exchange(ref _pipe, null!);

        // Should we also complete the pipe ? I don't know since this should be done by the transport
        // that own the pipe, but this is not within the scope of this library so maybe we should...
        try
        {
            pipe?.CancelPendingFlush();
        }
        catch
        {
            /* discard all exceptions at this point */
        }
        finally
        {
            semaphore?.Dispose();
        }

        GC.SuppressFinalize(this);
    }
}

public class PipeFrameEncoder<TMeta> : PipeFrameEncoder, IFrameEncoder<TMeta>
    where TMeta : class, IFrameMetadata
{
    protected PipeFrameEncoder(PipeWriter pipe, IMetadataEncoder encoder, SemaphoreSlim? singleWriter = default) : base(pipe, encoder, singleWriter)
    {
    }

    protected PipeFrameEncoder(Stream stream, IMetadataEncoder encoder, SemaphoreSlim? singleWriter = default) : base(stream, encoder, singleWriter)
    {
    }

    public ValueTask WriteAsync(IAsyncEnumerable<Frame<TMeta>> frames, CancellationToken token = default)
    {
        var writer = _pipe ?? throw new ObjectDisposedException(nameof(PipeFrameDecoder<TMeta>));

        // try to get the conch; if not, switch to async
        return TryWaitForSingleWriter(token) ? sendAll() : SendAllSlow();

        async ValueTask sendAll()
        {
            var framesWritten = 0;
            try
            {
                await foreach (var frame in frames.WithCancellation(token))
                {
                    var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);

                    var flushResult = writeAsync.IsCompletedSuccessfully
                        ? writeAsync.Result
                        : await writeAsync.ConfigureAwait(false);

                    framesWritten++;

                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                        break;
                }
            }
            finally
            {
                Release(framesWritten);
            }
        }

        async ValueTask SendAllSlow()
        {
            await WaitForSingleWriterAsync(token).ConfigureAwait(false);

            var framesWritten = 0;

            try
            {
                await foreach (var frame in frames.WithCancellation(token))
                {
                    var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);
                    var flushResult = writeAsync.IsCompletedSuccessfully
                        ? writeAsync.Result
                        : await writeAsync.ConfigureAwait(false);

                    framesWritten++;

                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                        break;
                }
            }
            finally
            {
                Release(framesWritten);
            }
        }
    }

    public ValueTask WriteAsync(IEnumerable<Frame<TMeta>> frames, CancellationToken token = default)
    {
        var writer = _pipe ?? throw new ObjectDisposedException(nameof(PipeFrameDecoder<TMeta>));

        // try to get the conch; if not, switch to async
        return TryWaitForSingleWriter(token) ? SendAll() : SendAllSlow();

        async ValueTask SendAll()
        {
            var framesWritten = 0;
            try
            {
                foreach (var frame in frames)
                {
                    var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);

                    var flushResult = writeAsync.IsCompletedSuccessfully
                        ? writeAsync.Result
                        : await writeAsync.ConfigureAwait(false);

                    framesWritten++;

                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                        break;
                }
            }
            finally
            {
                Release(framesWritten);
            }
        }

        async ValueTask SendAllSlow()
        {
            await WaitForSingleWriterAsync(token).ConfigureAwait(false);

            var framesWritten = 0;

            try
            {
                foreach (var frame in frames)
                {
                    var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);

                    var flushResult = writeAsync.IsCompletedSuccessfully
                        ? writeAsync.Result
                        : await writeAsync.ConfigureAwait(false);

                    framesWritten++;

                    if (flushResult.IsCanceled || flushResult.IsCompleted)
                        break;
                }
            }
            finally
            {
                Release(framesWritten);
            }
        }
    }

    public ValueTask WriteAsync(in Frame<TMeta> frame, CancellationToken token = default)
    {
        var writer = _pipe ?? throw new ObjectDisposedException(nameof(PipeFrameDecoder<TMeta>));

        // try to get the conch; if not, switch to async
        if (!TryWaitForSingleWriter(token)) return SendAsyncSlowPath(frame);

        var release = true;
        try
        {
            var write = writer.WriteFrameAsync(_encoder, in frame, token);

            if (write.IsCompletedSuccessfully)
                return default;

            release = false;
            return AwaitFlushAndRelease(write);
        }
        finally
        {
            if (release)
                Release();
        } // don't release here if we had to continue with an async path

        async ValueTask AwaitFlushAndRelease(ValueTask<FlushResult> flush)
        {
            try
            {
                await flush.ConfigureAwait(false);
            }
            finally
            {
                Release();
            }
        }

        async ValueTask SendAsyncSlowPath(Frame<TMeta> frm)
        {
            await WaitForSingleWriterAsync(token).ConfigureAwait(false);

            try
            {
                var writeAsync = writer.WriteFrameAsync(_encoder, in frm, token);

                if (!writeAsync.IsCompletedSuccessfully)
                    await writeAsync.ConfigureAwait(false);
            }
            finally
            {
                Release();
            }
        }
    }
}