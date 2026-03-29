using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Bubble.Core.Network.Framing.Abstractions.Metadata;
using Bubble.Core.Network.Framing.Serialization;

namespace Bubble.Core.Network.Framing.Extensions;

public static class PipeWriterExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IFrameMessageEncoder<TMetadata> AsFrameMessageEncoder<TMetadata>(
        this PipeWriter w, MetadataParser<TMetadata> encoder, IMessageWriter writer, SemaphoreSlim? singleWriter = default)
        where TMetadata : class, IFrameMetadata
    {
        return new PipeMessageEncoder<TMetadata>(w, encoder, writer, singleWriter);
    }
}