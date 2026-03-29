namespace Bubble.SourceGenerators.Infrastructure.Extensions;

public static class CollectionExtensions
{
    public static IEnumerable<T> DistinctBy<T>(this IEnumerable<T> items, Func<T, T, bool> predicate)
    {
        var list = new List<T>();

        foreach (var item in items)
        {
            if (list.Any(x => predicate(x, item)))
                continue;

            list.Add(item);
        }

        return list;
    }
}