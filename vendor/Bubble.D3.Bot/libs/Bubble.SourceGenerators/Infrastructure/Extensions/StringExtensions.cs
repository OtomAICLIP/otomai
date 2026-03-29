using System.Text;

namespace Bubble.SourceGenerators.Infrastructure.Extensions;

public static class StringExtensions
{
    public static int CountOccurrencesOfName(this string value, string name)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(name, index, StringComparison.Ordinal)) is not -1)
        {
            index += name.Length;
            count++;
        }

        return count;
    }

    public static string GetLastSegment(this string value, char separator)
    {
        var index = value.LastIndexOf(separator);

        return index is -1
            ? value
            : value[(index + 1)..];
    }

    public static string ToCamelCase(this string value)
    {
        return value.Length switch
        {
            0 => value,
            1 => char.ToLowerInvariant(value[0]).ToString(),
            _ => char.ToLowerInvariant(value[0]) + value[1..]
        };
    }

    public static string ToSnakeCase(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var builder = new StringBuilder(value.Length + 10);

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];

            if (char.IsUpper(c))
            {
                if (i > 0)
                    builder.Append('_');

                builder.Append(char.ToLowerInvariant(c));
            }
            else
                builder.Append(c);
        }

        return builder.ToString();
    }
}