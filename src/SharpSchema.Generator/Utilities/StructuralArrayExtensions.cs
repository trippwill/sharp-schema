using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

/// <summary>
/// Extension methods for <see cref="StructuralArray{T}"/>.
/// </summary>
public static class StructuralArrayExtensions
{
    /// <summary>
    /// Projects each element of a <see cref="StructuralArray{T}"/> into a new form.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the source array.</typeparam>
    /// <param name="array">A <see cref="StructuralArray{T}"/> to project.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> whose elements are the result of invoking the transform function on each element of the source array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the selector function is null.</exception>
    public static IEnumerable<T> Select<T>(this StructuralArray<T> array, Func<T, T> selector)
        where T : notnull, SchemaNode.ISchemaNode
    {
        Throw.IfNullArgument(selector, nameof(selector));
        for (var i = 0; i < array.Count; i++)
        {
            yield return selector(array[i]);
        }
    }

    /// <summary>
    /// Projects each element of a <see cref="StructuralArray{TSource}"/> into a new form.
    /// </summary>
    /// <typeparam name="TSource">The type of the elements of the source array.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by the transform function.</typeparam>
    /// <param name="array">A <see cref="StructuralArray{TSource}"/> to project.</param>
    /// <param name="selector">A transform function to apply to each element.</param>
    /// <returns>An <see cref="IEnumerable{TResult}"/> whose elements are the result of invoking the transform function on each element of the source array.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the selector function is null.</exception>
    public static IEnumerable<TResult> Select<TSource, TResult>(this StructuralArray<TSource> array, Func<TSource, TResult> selector)
        where TSource : notnull, SchemaNode.ISchemaNode
    {
        Throw.IfNullArgument(selector, nameof(selector));
        for (var i = 0; i < array.Count; i++)
        {
            yield return selector(array[i]);
        }
    }
}
