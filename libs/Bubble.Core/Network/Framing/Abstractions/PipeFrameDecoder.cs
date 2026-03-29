using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Bubble.Core.Network.Framing.Abstractions.Extensions;
using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Abstractions;

public class PipeFrameDecoder : IFrameDecoder
{
    protected readonly IMetadataDecoder _decoder;

    protected Frame _frame;
    protected long _framesRead;
    protected bool _hasAdvanced = true;
    protected bool _isCanceled;
    protected bool _isCompleted;
    protected bool _isConsuming;
    protected bool _isThisCompleted;
    protected SequencePosition _nextFrame;
    protected PipeReader? _pipe;

    public long FramesRead =>
        Interlocked.Read(ref _framesRead);

    protected PipeFrameDecoder(Stream stream, IMetadataDecoder decoder)
    {
        _decoder = decoder;
        _pipe = PipeReader.Create(stream);
    }

    protected PipeFrameDecoder(PipeReader pipe, IMetadataDecoder decoder)
    {
        _decoder = decoder;
        _pipe = pipe;
    }

    private async ValueTask<Frame> ReadFrameAsync(bool throwOnConsuming, CancellationToken token = default)
    {
        if (throwOnConsuming && _isConsuming)
            ThrowIfAlreadyConsuming();

        ObjectDisposedException.ThrowIf(_isCompleted || _isThisCompleted, nameof(PipeFrameDecoder));
        ObjectDisposedException.ThrowIf(!TryAdvanceToNextFrame(), nameof(PipeFrameDecoder));

        var reader = _pipe ?? throw new ObjectDisposedException(nameof(PipeFrameDecoder));

        try
        {
            for (var attempt = 1;; attempt++)
            {
                var readResult = await reader.ReadAsync(token).ConfigureAwait(false);

                if (TryReadFrame(in readResult, out _frame))
                    return _frame;

                Trace.TraceWarning($"Couldn't read frame with a single attempt, current try: {attempt}");
            }
        }
        catch (InvalidOperationException e) when (e.Message is "Reading is not allowed after reader was completed.")
        {
            throw new ObjectDisposedException(nameof(PipeFrameDecoder), e);
        }
    }

    private static void ThrowIfAlreadyConsuming()
    {
        throw new InvalidOperationException("Reading is not allowed while consuming frames via IAsyncEnumerable.");
    }

    protected bool TryAdvanceToNextFrame()
    {
        if (_hasAdvanced)
            return true;

        if (_pipe is null)
            return false;

        if (!_pipe.TryAdvanceTo(_nextFrame))
            return false;

        _hasAdvanced = true;
        return true;
    }

    protected bool TryReadFrame(in ReadResult readResult, out Frame frame)
    {
        _isCompleted = readResult is { IsCompleted: true, Buffer.IsEmpty: true, IsCanceled: false };
        _isCanceled = readResult.IsCanceled;

        frame = default;

        if (_isCanceled)
            return false;

        var buffer = readResult.Buffer;

        _nextFrame = buffer.Start;
        _hasAdvanced = false;

        if (buffer.TryParseFrame(_decoder, out frame))
        {
            _nextFrame = frame.Payload.End;
            _framesRead++;
            // If the payload is empty there's no need for the reader to hold on to the bytes.
            return !frame.IsPayloadEmpty() || TryAdvanceToNextFrame();
        }

        if (_isCompleted && !buffer.IsEmpty)
            throw new InvalidDataException("Connection terminated while reading a message.");

        TryAdvanceToNextFrame();

        frame = default;
        return false;
    }

    public ValueTask<Frame> ReadFrameAsync(CancellationToken token = default)
    {
        return ReadFrameAsync(true, token);
    }

    public async IAsyncEnumerable<Frame> ReadFramesAsync([EnumeratorCancellation] CancellationToken token = default)
    {
        if (_isConsuming)
            ThrowIfAlreadyConsuming();

        _isConsuming = true;

        try
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ReadFrameAsync(false, token).ConfigureAwait(false);
                }
                catch (ObjectDisposedException)
                {
                    yield break;
                }

                yield return _frame;
            }
        }
        finally
        {
            _isConsuming = false;
        }
    }

    #pragma warning disable CA1816 // GC.SuppressFinalize is already called in Dispose
    public virtual ValueTask DisposeAsync()
        #pragma warning restore CA1816 // GC.SuppressFinalize is already called in Dispose
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    public virtual void Dispose()
    {
        var pipe = Interlocked.Exchange(ref _pipe, null);

        if (pipe is null)
            return;

        _isThisCompleted = true;

        // Should we also complete the pipe ? I don't know since this should be done by the transport
        // that own the pipe, but this is not within the scope of this library so maybe we should...
        pipe.CancelPendingRead();
        GC.SuppressFinalize(this);
    }
}

public class PipeFrameDecoder<TMeta> : PipeFrameDecoder, IFrameDecoder<TMeta>
    where TMeta : class, IFrameMetadata
{
    protected PipeFrameDecoder(PipeReader pipe, IMetadataDecoder decoder) : base(pipe, decoder)
    {
    }

    protected PipeFrameDecoder(Stream stream, IMetadataDecoder decoder) : base(stream, decoder)
    {
    }

    public new ValueTask<Frame<TMeta>> ReadFrameAsync(CancellationToken token = default)
    {
        var readAsync = base.ReadFrameAsync(token);

        return readAsync.IsCompletedSuccessfully
            ? ValueTask.FromResult(readAsync.Result.AsTyped<TMeta>())
            : AwaitAndReturn(readAsync);

        static async ValueTask<Frame<TMeta>> AwaitAndReturn(ValueTask<Frame> readTask)
        {
            var r = await readTask.ConfigureAwait(false);
            return r.AsTyped<TMeta>();
        }
    }

    public new async IAsyncEnumerable<Frame<TMeta>> ReadFramesAsync([EnumeratorCancellation] CancellationToken token = default)
    {
        await foreach (var frame in base.ReadFramesAsync(token))
            yield return frame.AsTyped<TMeta>();
    }
}