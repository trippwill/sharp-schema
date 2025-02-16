using System.Runtime.InteropServices;

namespace SharpSchema.Generator.Utilities;

/// <summary>
/// Provides extension methods for string operations.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Computes a hash code for the given string using a stable algorithm.
    /// </summary>
    /// <remarks>
    ///     A <see langword="null"/> <see langword="string"/> always returns <c>0</c>. <br />
    ///     An empty <see langword="string"/> always returns the initial hash code <c>0x1505L</c>.
    /// </remarks>
    /// <param name="value">The input string as a ReadOnlySpan of characters.</param>
    /// <returns>A long representing the hash code of the input string.</returns>
    public static long GetStableHash(this string? value)
    {
        if (value is null) return 0;
        return GetStableHash(MemoryMarshal.AsBytes(value.AsSpan()));
    }

    /// <summary>
    /// Computes a hash code for the given ReadOnlySpan of characters using a stable algorithm.
    /// </summary>
    /// <param name="chars">The input ReadOnlySpan of characters.</param>
    /// <returns>A long representing the hash code of the input characters.</returns>
    public static long GetStableHash(this ReadOnlySpan<char> chars)
    {
        return GetStableHash(MemoryMarshal.AsBytes(chars));
    }

    /// <summary>
    /// Computes a hash code for the given ReadOnlySpan of bytes using a stable algorithm.
    /// </summary>
    /// <param name="bytes">The input ReadOnlySpan of bytes.</param>
    /// <returns>A long representing the hash code of the input bytes.</returns>
    public static long GetStableHash(this ReadOnlySpan<byte> bytes)
    {
        unchecked
        {
            long hash = 0x1505L;
            foreach (byte b in bytes)
            {
                hash = (hash << 5) + hash ^ b;
            }
            return hash;
        }
    }
}
