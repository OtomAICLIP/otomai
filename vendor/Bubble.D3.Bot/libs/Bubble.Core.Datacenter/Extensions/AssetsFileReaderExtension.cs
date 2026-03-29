using AssetsTools.NET;

namespace Bubble.Core.Datacenter.Extensions;

public static class AssetsFileReaderExtension
{
    public static bool ReadBoolean(this AssetsFileReader reader, bool align)
    {
        var data = reader.ReadBoolean();

        if (align)
            reader.Align();

        return data;
    }

    public static byte ReadByte(this AssetsFileReader reader, bool align)
    {
        var data = reader.ReadByte();

        if (align)
            reader.Align();

        return data;
    }

    public static char ReadChar(this AssetsFileReader reader, bool align)
    {
        var data = reader.ReadChar();

        if (align)
            reader.Align();

        return data;
    }

    public static string ReadCountStringInt32(this AssetsFileReader reader, bool align)
    {
        var data = reader.ReadCountStringInt32();

        if (align)
            reader.Align();

        return data;
    }

    public static string ReadCountStringInt32Aligned(this AssetsFileReader reader)
    {
        return reader.ReadCountStringInt32Aligned(true);
    }
    
    public static string ReadCountStringInt32Aligned(this AssetsFileReader reader, bool align)
    {
        return reader.ReadCountStringInt32(align);
    }

    public static short ReadInt16(this AssetsFileReader reader, bool align)
    {
        var data = reader.ReadInt16();

        if (align)
            reader.Align();

        return data;
    }


    public static List<T> ReadList<T>(this AssetsFileReader reader, Func<T> action, bool align)
    {
        var size = reader.ReadInt32();
        var list = new List<T>(size);

        for (var i = 0; i < size; i++)
        {
            list.Add(action());
        }
        if (align)
            reader.Align();

        return list;
    }

    public static sbyte ReadSByte(this AssetsFileReader reader, bool align)
    {
        var data = reader.ReadSByte();

        if (align)
            reader.Align();

        return data;
    }

    public static ushort ReadUInt16(this AssetsFileReader reader, bool align)
    {
        var data = reader.ReadUInt16();

        if (align)
            reader.Align();

        return data;
    }
}