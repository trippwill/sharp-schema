namespace SharpSchema.Generator.Utilities;

internal static class CollectionExtensions
{
    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
            action(item);
    }

    public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue>pair, out TKey key, out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}
