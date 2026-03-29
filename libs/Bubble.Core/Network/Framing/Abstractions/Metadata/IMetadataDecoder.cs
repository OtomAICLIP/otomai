using System.Buffers;

namespace Bubble.Core.Network.Framing.Abstractions.Metadata;

public interface IMetadataDecoder
{
    int GetMetadataLength(IFrameMetadata metadata);

    bool TryParse(ref SequenceReader<byte> input, out IFrameMetadata? metadata);
}