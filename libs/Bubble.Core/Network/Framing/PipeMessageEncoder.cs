using System.Buffers;
using System.IO.Pipelines;
using Bubble.Core.Network.Framing.Abstractions;
using Bubble.Core.Network.Framing.Abstractions.Metadata;
using Bubble.Core.Network.Framing.Serialization;

namespace Bubble.Core.Network.Framing;

public sealed class PipeMessageEncoder<TMeta> : PipeFrameEncoder<TMeta>, IFrameMessageEncoder<TMeta>
    where TMeta : class, IFrameMetadata
{
    private readonly IMessageWriter _serializer;

    public PipeMessageEncoder(PipeWriter pipe, IMetadataEncoder encoder, IMessageWriter serializer, SemaphoreSlim? singleWriter = default) : base(pipe, encoder, singleWriter)
    {
        _serializer = serializer;
    }

    public PipeMessageEncoder(Stream stream, IMetadataEncoder encoder, IMessageWriter serializer, SemaphoreSlim? singleWriter = default) : base(stream, encoder, singleWriter)
    {
        _serializer = serializer;
    }

    public ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken token = default)
    {
        var writer = _pipe ?? throw new ObjectDisposedException(nameof(PipeMessageEncoder<TMeta>));

        // try to get the conch; if not, switch to async
        if (!TryWaitForSingleWriter(token))
            return SendAsyncSlowPath(data);

        var release = true;

        try
        {
            writer.Write(data.Span);

            var flush = writer.FlushAsync(token); // includes a flush

            if (flush.IsCompletedSuccessfully)
                return default;

            release = false;
            return AwaitFlushAndRelease(flush);
        }
        finally
        {
            if (release)
                Release();
        } // don't release here if we had to continue with an async path

        async ValueTask AwaitFlushAndRelease(ValueTask<FlushResult> f)
        {
            try
            {
                await f.ConfigureAwait(false);
            }
            finally
            {
                Release();
            }
        }

        async ValueTask SendAsyncSlowPath(ReadOnlyMemory<byte> bin)
        {
            await WaitForSingleWriterAsync(token).ConfigureAwait(false);

            try
            {
                writer.Write(bin.Span);

                var flush = writer.FlushAsync(token); // includes a flush

                if (!flush.IsCompletedSuccessfully)
                    await flush.ConfigureAwait(false);
            }
            finally
            {
                Release();
            }
        }
    }

    public ReadOnlyMemory<byte> Encode<TMessage>(in TMessage message)
    {
        var arrayBufferWriter = new ArrayBufferWriter<byte>(1024);
        _serializer.Encode(message, arrayBufferWriter);
        
        return arrayBufferWriter.WrittenMemory.ToArray();
    }
    
    public ValueTask WriteAsync<TMessage>(in TMessage message, CancellationToken token = default)
    {
        var writer = _pipe ?? throw new ObjectDisposedException(nameof(PipeMessageEncoder<TMeta>));

        // try to get the conch; if not, switch to async
        if (!TryWaitForSingleWriter(token))
            return SendAsyncSlowPath(message);

        var release = true;

        try
        {
            _serializer.Encode(message, writer);

            var flush = writer.FlushAsync(token); // includes a flush

            if (flush.IsCompletedSuccessfully)
                return default;

            release = false;
            return AwaitFlushAndRelease(flush);
        }
        finally
        {
            if (release)
                Release();
        } // don't release here if we had to continue with an async path

        async ValueTask AwaitFlushAndRelease(ValueTask<FlushResult> f)
        {
            try
            {
                await f.ConfigureAwait(false);
            }
            finally
            {
                Release();
            }
        }

        async ValueTask SendAsyncSlowPath(TMessage msg)
        {
            await WaitForSingleWriterAsync(token).ConfigureAwait(false);

            try
            {
                _serializer.Encode(msg, writer);

                var flush = writer.FlushAsync(token); // includes a flush

                if (!flush.IsCompletedSuccessfully)
                    await flush.ConfigureAwait(false);
            }
            finally
            {
                Release();
            }
        }
    }
}