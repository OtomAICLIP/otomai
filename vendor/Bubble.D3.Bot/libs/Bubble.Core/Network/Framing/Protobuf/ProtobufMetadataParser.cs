using System.Buffers;
using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Protobuf;

public sealed class ProtobufMetadataParser : MetadataParser<ProtobufMetadata>
{
    protected override int GetLength(ProtobufMetadata metadata)
    {
        var size = 0;
        var value = metadata.Length;

        do
        {
            size++;
            value >>= 7;
        }
        while (value is not 0);

        return size;
    }

    protected override bool TryParse(ref SequenceReader<byte> input, out ProtobufMetadata? metadata)
    {
        metadata = null;

        if (!TryReadVarInt32(ref input, out var length))
            return false;

        metadata = new ProtobufMetadata(length);
        return true;
    }

    public static bool TryReadVarInt32(ref SequenceReader<byte> reader, out int value)
    {
        value = 0;
        var shift = 0;
        byte b;

        do
        {
            if (!reader.TryRead(out b))
                return false;

            value |= (b & 0x7F) << shift;
            shift += 7;
        }
        while ((b & 0x80) is not 0);

        return true;
    }

    protected override void Write(ref Span<byte> span, ProtobufMetadata metadata)
    {
        WriteVarInt32(ref span, metadata.Length);
    }

    public static void WriteVarInt32(ref Span<byte> span, int value)
    {
        var i = 0;

        do
        {
            var b = value & 0x7F;

            value >>= 7;

            if (value is not 0)
                b |= 0x80;

            span[i] = (byte)b;
            i++;
        }
        while (value is not 0);
    }
}