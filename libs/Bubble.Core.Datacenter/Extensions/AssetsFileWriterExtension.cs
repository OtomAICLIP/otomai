using AssetsTools.NET;

namespace Bubble.Core.Datacenter.Extensions;

public static class AssetsFileWriterExtension
{
    public static void Write(this AssetsFileWriter writer, int value, bool align)
    {
        writer.Write(value);

        if (align)
            writer.Align();
    }

    public static void Write(this AssetsFileWriter writer, short value, bool align)
    {
        writer.Write(value);

        if (align)
            writer.Align();
    }

    public static void Write(this AssetsFileWriter writer, byte value, bool align)
    {
        writer.Write(value);

        if (align)
            writer.Align();
    }

    public static void Write(this AssetsFileWriter writer, char value, bool align)
    {
        writer.Write(value);

        if (align)
            writer.Align();
    }

    public static void Write(this AssetsFileWriter writer, sbyte value, bool align)
    {
        writer.Write(value);

        if (align)
            writer.Align();
    }

    public static void Write(this AssetsFileWriter writer, bool value, bool align)
    {
        writer.Write(value);

        if (align)
            writer.Align();
    }

    public static void Write(this AssetsFileWriter writer, string value, bool align)
    {
        writer.Write(value);

        if (align)
            writer.Align();
    }

    public static void Write(this AssetsFileWriter writer, float value, bool align)
    {
        writer.Write(value);

        if (align)
            writer.Align();
    }

    public static void Write(this AssetsFileWriter writer, long value, bool align)
    {
        writer.Write(value);

        if (align)
            writer.Align();
    }

    public static void Write(this AssetsFileWriter writer, ushort value, bool align)
    {
        writer.Write(value);

        if (align)
            writer.Align();
    }

    public static void WriteCountStringInt32(this AssetsFileWriter writer, string value, bool align)
    {
        writer.WriteCountStringInt32(value);

        if (align)
            writer.Align();
    }
    public static void WriteCountStringInt32Aligned(this AssetsFileWriter writer, string value)
    {
        writer.WriteCountStringInt32Aligned(value, true);
    }
    
    public static void WriteCountStringInt32Aligned(this AssetsFileWriter writer, string value, bool align)
    {
        writer.WriteCountStringInt32(value);

        if (align)
            writer.Align();
    }
    
    public static void WriteList<T>(this AssetsFileWriter writer, List<T> list, Action<T> action, bool align)
    {
        writer.Write(list.Count);

        foreach (var item in list)
            action(item);

        if (align)
            writer.Align();
    }
}