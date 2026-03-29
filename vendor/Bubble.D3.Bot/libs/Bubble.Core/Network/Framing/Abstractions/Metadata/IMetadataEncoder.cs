namespace Bubble.Core.Network.Framing.Abstractions.Metadata;

public interface IMetadataEncoder
{
    int GetLength(IFrameMetadata metadata);

    void Write(ref Span<byte> span, IFrameMetadata metadata);
}