namespace Bubble.Core.Extensions;

public static class CollectionsExtensions
{
    public static bool CompareEnumerable<T>(this IEnumerable<T> ie1, IEnumerable<T> ie2)
    {
        if (ie1.GetType() != ie2.GetType())
            return false;

        var a1 = ie1.ToArray();
        var a2 = ie2.ToArray();

        if (a1.Length != a2.Length)
            return false;

        return !a1.Except(a2).Any() && !a2.Except(a1).Any();
    }

    public static int IndexOf<T>(this ReadOnlySpan<T> span, T value, int start)
    {
        for (var i = start; i < span.Length; i++)
            if (EqualityComparer<T>.Default.Equals(span[i], value))
                return i;

        return -1;
    }

    public static T? RandomElementOrDefault<T>(this IEnumerable<T> enumerable)
    {
        var enumerable1 = enumerable as T[] ?? enumerable.ToArray();
        var len = enumerable1.Length;

        return len <= 0 ? default : enumerable1[Random.Shared.Next(len)];
    }

    public static T? RandomElementOrDefault<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
    {
        var enumerable1 = enumerable as T[] ?? enumerable.ToArray();
        var len = enumerable1.Length;

        return len <= 0 ? default : enumerable1.Where(predicate).RandomElementOrDefault();
    }
}