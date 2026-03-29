namespace Bubble.Core.Datacenter.Extensions;

public static class StringExtension
{
    public static string Capitalize(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        if (str.StartsWith("m_"))
            return str;

        return char.ToUpper(str[0]) + str[1..];
    }

    public static string Uncapitalize(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        if (str.StartsWith("m_"))
            return str;

        return char.ToLower(str[0]) + str[1..];
    }
}