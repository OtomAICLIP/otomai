using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Abstractions.Extensions;

public static class PipeWriterExtensions
{
    private static void WriteBigMemory(this PipeWriter writer, ReadOnlyMemory<byte> buffer)
    {
        var i = 0;

        const int chunkSize = 1024 * 8;

        for (var c = buffer.Length / chunkSize; i < c; i++)
        {
            var memory = writer.GetMemory(chunkSize);
            buffer.Slice(i * chunkSize, chunkSize).CopyTo(memory);
            writer.Advance(chunkSize);
        }

        var remaining = buffer.Length % chunkSize;

        if (remaining == 0)
            return;

        var mem = writer.GetMemory(remaining);
        buffer[^remaining..].CopyTo(mem);
        writer.Advance(remaining);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<FlushResult> WriteFrameAsync(this PipeWriter writer, IMetadataEncoder encoder, in Frame frame, CancellationToken token = default)
    {
        return !encoder.TryWriteMetadata(writer, frame.Metadata)
            // Returns a completed flushResult in case we couldn't write the metadata
            ? ValueTask.FromResult(new FlushResult(token.IsCancellationRequested, true))
            : frame.IsPayloadEmpty()
                ? writer.FlushAsync(token)
                : !frame.Payload.IsSingleSegment
                    ? writer.WriteMultiSegmentSequenceAsync(frame.Payload, token)
                    : writer.WriteMemoryAsync(frame.Payload.First, token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<FlushResult> WriteFrameAsync<TMeta>(this PipeWriter writer, IMetadataEncoder encoder, in Frame<TMeta> frame, CancellationToken token = default)
        where TMeta : class, IFrameMetadata
    {
        // Returns a completed flushResult in case we couldn't write the metadata
        if (!encoder.TryWriteMetadata(writer, frame.Metadata))
            return ValueTask.FromResult(new FlushResult(token.IsCancellationRequested, true));

        return frame.IsPayloadEmpty()
            ? writer.FlushAsync(token)
            : !frame.Payload.IsSingleSegment
                ? writer.WriteMultiSegmentSequenceAsync(frame.Payload, token)
                : writer.WriteMemoryAsync(frame.Payload.First, token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<FlushResult> WriteMemoryAsync(this PipeWriter writer, ReadOnlyMemory<byte> buffer, CancellationToken token = default)
    {
        const int chunkSize = 1024 * 8;

        return buffer.Length < chunkSize
            ? writer.WriteAsync(buffer, token)
            : writeBigMemoryAsync();

        ValueTask<FlushResult> writeBigMemoryAsync()
        {
            writer.WriteBigMemory(buffer);
            return writer.FlushAsync(token);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueTask<FlushResult> WriteMultiSegmentSequenceAsync(this PipeWriter writer, ReadOnlySequence<byte> buffer, CancellationToken token = default)
    {
        foreach (var segment in buffer)
            writer.WriteBigMemory(segment);

        return writer.FlushAsync(token);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<FlushResult> WriteSequenceAsync(
        this PipeWriter writer, ReadOnlySequence<byte> buffer,
        CancellationToken token = default)
    {
        return !buffer.IsSingleSegment
            ? writer.WriteMultiSegmentSequenceAsync(buffer, token)
            : writer.WriteMemoryAsync(buffer.First, token);
    }
}