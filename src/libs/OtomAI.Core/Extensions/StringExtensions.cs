namespace OtomAI.Core.Extensions;

/// <summary>
/// String extension methods. Mirrors Bubble.Core.Extensions.
/// </summary>
public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    public static string ToSnakeCase(this string value)
    {
        return string.Concat(value.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLower(c) : char.ToLower(c).ToString()));
    }
}
