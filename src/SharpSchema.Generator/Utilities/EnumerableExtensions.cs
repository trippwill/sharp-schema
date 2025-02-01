using System.Collections.Immutable;

namespace SharpSchema.Generator.Utilities;

internal static class EnumerableExtensions
{
    public static ImmutableArray<TResult> SelectNotNull<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
        where TResult : notnull
    {
        ImmutableArray<TResult>.Builder builder = ImmutableArray.CreateBuilder<TResult>(source.Count());
        foreach (TSource item in source)
        {
            TResult? result = selector(item);
            if (result is not null)
                builder.Add(result);
        }

        return builder.ToImmutable();
    }

    public static ImmutableArray<TResult> SelectNotNull<TSource, TResult>(this IReadOnlyCollection<TSource> source, Func<TSource, TResult?> selector)
        where TResult : notnull
    {
        ImmutableArray<TResult>.Builder builder = ImmutableArray.CreateBuilder<TResult>(source.Count);
        foreach (TSource item in source)
        {
            TResult? result = selector(item);
            if (result is not null)
                builder.Add(result);
        }

        return builder.ToImmutable();
    }

    public static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}
