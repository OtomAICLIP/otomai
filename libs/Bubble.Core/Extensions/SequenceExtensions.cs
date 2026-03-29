using System.Buffers;

namespace Bubble.Core.Extensions;

public class SequenceExtensions
{
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
        while ((b & 0x80) != 0);

        return true;
    }

    public static int WriteVarInt32(ref Span<byte> span, int value)
    {
        var i = 0;

        do
        {
            var b = value & 0x7F;

            value >>= 7;

            if (value != 0)
                b |= 0x80;

            span[i] = (byte)b;
            i++;
        }
        while (value != 0);

        return i;
    }
}