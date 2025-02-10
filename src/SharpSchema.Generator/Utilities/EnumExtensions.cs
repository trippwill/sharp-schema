using System.Runtime.CompilerServices;

namespace SharpSchema.Generator.Utilities;

internal static class EnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckFlag(this Accessibilities value, Accessibilities flag) => (value & flag) == flag;

    public static bool CheckFlag(this Traversal value, Traversal flag) => (value & flag) == flag;
}
