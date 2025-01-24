using System.Runtime.CompilerServices;

namespace SharpSchema.Generator.Utilities;
internal static class SchemaHash
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Combine(long hash1, long hash2)
    {
        unchecked
        {
            return (hash1 << 5) + hash1 ^ hash2;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Combine(long hash1, long hash2, long hash3)
    {
        return Combine(Combine(hash1, hash2), hash3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Combine(long hash1, long hash2, long hash3, long hash4)
    {
        return Combine(Combine(hash1, hash2), Combine(hash3, hash4));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Combine(long hash1, long hash2, long hash3, long hash4, long hash5)
    {
        return Combine(Combine(hash1, hash2, hash3, hash4), hash5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long Combine(long hash1, long hash2, long hash3, long hash4, long hash5, long hash6)
    {
        return Combine(Combine(hash1, hash2, hash3, hash4), Combine(hash5, hash6));
    }
}
