using System.Collections.Immutable;

namespace SharpSchema.Generator;

internal static class EnumerableExtensions
{

    public static IEnumerable<TResult> SelectNotNull<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
        where TResult : notnull
    {
        foreach (TSource item in source)
        {
            TResult? result = selector(item);
            if (result is not null)
                yield return result;
        }
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
}
