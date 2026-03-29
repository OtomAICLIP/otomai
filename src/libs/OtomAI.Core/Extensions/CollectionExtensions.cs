namespace OtomAI.Core.Extensions;

/// <summary>
/// Collection extension methods. Mirrors Bubble.Core.Extensions.
/// </summary>
public static class CollectionExtensions
{
    public static T? MinByOrDefault<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
        where TKey : IComparable<TKey>
    {
        using var e = source.GetEnumerator();
        if (!e.MoveNext()) return default;
        var best = e.Current;
        var bestKey = selector(best);
        while (e.MoveNext())
        {
            var key = selector(e.Current);
            if (key.CompareTo(bestKey) < 0)
            {
                best = e.Current;
                bestKey = key;
            }
        }
        return best;
    }

    public static void Shuffle<T>(this IList<T> list, Random? rng = null)
    {
        rng ??= Random.Shared;
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size)
    {
        var chunk = new List<T>(size);
        foreach (var item in source)
        {
            chunk.Add(item);
            if (chunk.Count == size)
            {
                yield return chunk.ToArray();
                chunk.Clear();
            }
        }
        if (chunk.Count > 0) yield return chunk.ToArray();
    }
}
