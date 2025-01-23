using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator.Model;

/// <summary>
/// Provides static methods for creating <see cref="StructuralArray{T}"/> instances.
/// </summary>
public static class StructuralArray
{
    /// <summary>
    /// Creates a new <see cref="StructuralArray{T}"/> from the specified values.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array.</typeparam>
    /// <param name="values">The values to include in the array.</param>
    /// <returns>A new <see cref="StructuralArray{T}"/> containing the specified values.</returns>
    public static StructuralArray<T> Create<T>(ReadOnlySpan<T> values)
        where T : notnull, SchemaNode.ISchemaNode
    {
        ImmutableArray<T>.Builder builder = ImmutableArray.CreateBuilder<T>(values.Length);
        foreach (T value in values)
        {
            builder.Add(value);
        }

        return builder;
    }
}

/// <summary>
/// Represents an immutable array that supports structural equality and comparison.
/// </summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
[CollectionBuilder(typeof(StructuralArray), nameof(StructuralArray.Create))]
public readonly struct StructuralArray<T> : IEquatable<StructuralArray<T>>, IEnumerable<T>, SchemaNode.ISchemaNode
    where T : notnull, SchemaNode.ISchemaNode
{
    private readonly ImmutableArray<T> _array;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructuralArray{T}"/> struct.
    /// </summary>
    /// <param name="array">The immutable array to wrap.</param>
    public StructuralArray(ImmutableArray<T> array)
    {
        _array = array;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StructuralArray{T}"/> struct.
    /// </summary>
    /// <param name="builder">The builder to create the immutable array from.</param>
    internal StructuralArray(ImmutableArray<T>.Builder builder)
    {
        _array = builder.ToImmutable();
    }

    /// <summary>
    /// Implicitly converts a <see cref="StructuralArray{T}"/> to an <see cref="ImmutableArray{T}"/>.
    /// </summary>
    /// <param name="array">The <see cref="StructuralArray{T}"/> to convert.</param>
    public static implicit operator ImmutableArray<T>(StructuralArray<T> array) => array._array;

    /// <summary>
    /// Implicitly converts an <see cref="ImmutableArray{T}"/> to a <see cref="StructuralArray{T}"/>.
    /// </summary>
    /// <param name="array">The <see cref="ImmutableArray{T}"/> to convert.</param>
    public static implicit operator StructuralArray<T>(ImmutableArray<T> array) => new(array);

    /// <summary>
    /// Implicitly converts an <see cref="ImmutableArray{T}.Builder"/> to a <see cref="StructuralArray{T}"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ImmutableArray{T}.Builder"/> to convert.</param>
    public static implicit operator StructuralArray<T>(ImmutableArray<T>.Builder builder)
    {
        Throw.IfNullArgument(builder, nameof(builder));
        return new(builder);
    }

    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>The element at the specified index.</returns>
    public readonly T this[int index] => _array[index];

    /// <summary>
    /// Gets the number of elements in the array.
    /// </summary>
    public readonly int Count => _array.Length;

    /// <summary>
    /// Returns an enumerator that iterates through the array.
    /// </summary>
    /// <returns>An enumerator for the array.</returns>
    public readonly ImmutableArray<T>.Enumerator GetEnumerator() => _array.GetEnumerator();

    /// <summary>
    /// Determines the index of a specific item in the array.
    /// </summary>
    /// <param name="item">The object to locate in the array.</param>
    /// <param name="index">The zero-based starting index of the search.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <param name="equalityComparer">The equality comparer to use.</param>
    /// <returns>The index of the item if found; otherwise, -1.</returns>
    public readonly int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer) =>
        _array.IndexOf(item, index, count, equalityComparer);

    /// <summary>
    /// Determines the last index of a specific item in the array.
    /// </summary>
    /// <param name="item">The object to locate in the array.</param>
    /// <param name="index">The zero-based starting index of the search.</param>
    /// <param name="count">The number of elements in the section to search.</param>
    /// <param name="equalityComparer">The equality comparer to use.</param>
    /// <returns>The last index of the item if found; otherwise, -1.</returns>
    public readonly int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer) =>
        _array.LastIndexOf(item, index, count, equalityComparer);

    /// <summary>
    /// Determines whether the specified <see cref="StructuralArray{T}"/> is equal to the current <see cref="StructuralArray{T}"/>.
    /// </summary>
    /// <param name="other">The <see cref="StructuralArray{T}"/> to compare with the current <see cref="StructuralArray{T}"/>.</param>
    /// <returns>true if the specified <see cref="StructuralArray{T}"/> is equal to the current <see cref="StructuralArray{T}"/>; otherwise, false.</returns>
    public readonly bool Equals(StructuralArray<T> other) => StructuralComparisons.StructuralEqualityComparer.Equals(_array, other._array);

    /// <summary>
    /// Determines whether the specified object is equal to the current <see cref="StructuralArray{T}"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current <see cref="StructuralArray{T}"/>.</param>
    /// <returns>true if the specified object is equal to the current <see cref="StructuralArray{T}"/>; otherwise, false.</returns>
    public override readonly bool Equals(object obj) => obj is StructuralArray<T> array && this.Equals(array);

    /// <summary>
    /// Serves as a hash function for the <see cref="StructuralArray{T}"/>.
    /// </summary>
    /// <returns>A hash code for the current <see cref="StructuralArray{T}"/>.</returns>
    public override readonly int GetHashCode() => StructuralComparisons.StructuralEqualityComparer.GetHashCode(_array);

    /// <inheritdoc />
    public long GetSchemaHash()
    {
        long hash = 1701;
        foreach (var item in _array)
        {
            hash = (hash << 5) - hash + item.GetSchemaHash();
        }

        return hash;
    }

    /// <inheritdoc/>
    public static bool operator ==(StructuralArray<T> left, StructuralArray<T> right) => left.Equals(right);

    /// <inheritdoc/>
    public static bool operator !=(StructuralArray<T> left, StructuralArray<T> right) => !(left == right);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_array).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_array).GetEnumerator();
}
