namespace Bubble.Core.Datacenter;

public sealed class LocalizationFile
{
    public required Dictionary<uint, string> Texts { get; init; }
    public required Dictionary<ulong, string> UiTexts { get; init; }

    public string GetText(uint key)
    {
        return Texts.GetValueOrDefault(key) ?? $"[Unknown text: {key}]";
    }

    public string GetText(string key)
    {
        return Texts.GetValueOrDefault(uint.Parse(key)) ?? $"[Unknown text: {key}]";
    }
}

public static class LocalizationReader
{
    public static LocalizationFile ReadFile(string file)
    {
        using var fs = new FileStream(file, FileMode.Open, FileAccess.Read);
        var reader = new BinaryReader(fs);

        var header = reader.ReadByte();
        var fileName = reader.ReadBytes(header);

        var count = reader.ReadUInt32();

        var indexes = new Dictionary<uint, uint>();
        var uiIndexes = new Dictionary<ulong, uint>();

        var texts = new Dictionary<uint, string>();
        var uiTexts = new Dictionary<ulong, string>();

        for (var i = 0; i < count; i++)
        {
            var key = reader.ReadUInt32();
            var cursor = reader.ReadUInt32();
            indexes[key] = cursor;
        }

        var uiCount = reader.ReadUInt32();
        for (var i = 0; i < uiCount; i++)
        {
            var key = reader.ReadUInt64();
            uiIndexes[key] = reader.ReadUInt32();
        }

        foreach (var index in indexes)
        {
            fs.Position = index.Value;
            var text = reader.ReadString();

            texts[index.Key] = text;
        }

        foreach (var index in uiIndexes)
        {
            fs.Position = index.Value;
            var text = reader.ReadString();

            uiTexts[index.Key] = text;
        }

        var localization = new LocalizationFile
        {
            Texts = texts,
            UiTexts = uiTexts
        };

        return localization;
    }
}