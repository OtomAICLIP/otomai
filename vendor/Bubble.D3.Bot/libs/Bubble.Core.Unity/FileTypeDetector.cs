using System.Text.RegularExpressions;
using AssetsTools.NET;

namespace Bubble.Core.Unity;

public partial class FileTypeDetector
{
    public static DetectedFileType DetectFileType(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        var reader = new AssetsFileReader(fs);

        return FileTypeDetector.DetectFileType(reader, 0);
    }

    public static DetectedFileType DetectFileType(AssetsFileReader reader, long startAddress)
    {
        if (reader.BaseStream.Length < 0x20)
            return DetectedFileType.Unknown;

        reader.Position = startAddress;
        var possibleBundleHeader = reader.ReadStringLength(7);
        reader.Position = startAddress + 0x08;
        var possibleFormat = reader.ReadInt32();

        reader.Position = startAddress + (possibleFormat >= 0x16 ? 0x30 : 0x14);

        var possibleVersion = "";
        char curChar;

        while (reader.Position < reader.BaseStream.Length && (curChar = (char)reader.ReadByte()) != 0x00)
        {
            possibleVersion += curChar;
            if (possibleVersion.Length > 0xFF)
                break;
        }

        if (possibleBundleHeader == "UnityFS")
            return DetectedFileType.BundleFile;

        var emptyVersion = FileTypeDetector.PossibleVersionRegex().Replace(possibleVersion, "");
        var fullVersion = FileTypeDetector.PossibleVersionRegexFull().Replace(possibleVersion, "");

        if (possibleFormat < 0xFF && emptyVersion.Length == 0 && fullVersion.Length >= 5)
            return DetectedFileType.AssetsFile;

        return DetectedFileType.Unknown;
    }

    [GeneratedRegex(@"[a-zA-Z0-9\.\n\-]")]
    private static partial Regex PossibleVersionRegex();

    [GeneratedRegex(@"[^a-zA-Z0-9\.\n\-]")]
    private static partial Regex PossibleVersionRegexFull();
}