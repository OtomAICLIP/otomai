using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Bubble.Core.Extensions;

public static partial class StringExtensions
{
    public static string ByteArrayToString(this byte[] bytes)
    {
        var output = new StringBuilder(bytes.Length);

        foreach (var t in bytes) output.Append(t.ToString("X2", CultureInfo.InvariantCulture));

        return output.ToString().ToLower(CultureInfo.InvariantCulture);
    }

    public static string Capitalize(this string str)
    {
        return string.IsNullOrEmpty(str)
            ? string.Empty
            : string.Concat(str[..1].ToUpper(CultureInfo.InvariantCulture), str.AsSpan(1));
    }

    public static string ConcatCopy(this string str, int times)
    {
        var builder = new StringBuilder(str.Length * times);

        for (var i = 0; i < times; i++) builder.Append(str);

        return builder.ToString();
    }

    public static int CountOccurences(this string str, char chr, int startIndex, int count)
    {
        var occurences = 0;

        for (var i = startIndex; i < startIndex + count; i++)
            if (str[i] == chr)
                occurences++;

        return occurences;
    }

    public static string GetLastSegment(this string value, char separator)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var indexOf = value.LastIndexOf(separator);

        return indexOf is -1 ? value : value[(indexOf + 1)..];
    }

    public static string GetMd5(this string encryptString)
    {
        var passByteCrypt = MD5.HashData(Encoding.UTF8.GetBytes(encryptString));
        return passByteCrypt.ByteArrayToString();
    }

    public static string GetMd5(this byte[] data)
    {
        var passByteCrypt = MD5.HashData(data);
        return passByteCrypt.ByteArrayToString();
    }

    public static string GetMd5(this Span<byte> data)
    {
        var passByteCrypt = MD5.HashData(data);
        return passByteCrypt.ByteArrayToString();
    }

    public static string GetMd5(this ReadOnlySpan<byte> data)
    {
        var passByteCrypt = MD5.HashData(data);
        return passByteCrypt.ByteArrayToString();
    }

    public static string ToSnakeCase(this string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        return UnderscoreRegex().Match(input) + UnderscoreReplace().Replace(input, "$1_$2").ToLower(CultureInfo.InvariantCulture);
    }

    public static string UnCapitalize(this string str)
    {
        return string.IsNullOrEmpty(str)
            ? string.Empty
            : string.Concat(str[..1].ToLower(CultureInfo.InvariantCulture), str.AsSpan(1));
    }

    [GeneratedRegex("^_+", RegexOptions.Compiled, 1000)]
    private static partial Regex UnderscoreRegex();

    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.Compiled, 1000)]
    private static partial Regex UnderscoreReplace();
}