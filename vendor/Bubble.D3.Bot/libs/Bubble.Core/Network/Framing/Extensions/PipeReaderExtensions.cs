using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using Bubble.Core.Network.Framing.Abstractions.Metadata;
using Bubble.Core.Network.Framing.Serialization;

namespace Bubble.Core.Network.Framing.Extensions;

public static class PipeReaderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IFrameMessageDecoder<TMetadata> AsFrameMessageDecoder<TMetadata>(this PipeReader r, IMetadataDecoder decoder, IMessageReader<TMetadata> reader)
        where TMetadata : class, IFrameMetadata
    {
        return new PipeMessageDecoder<TMetadata>(r, decoder, reader);
    }
}