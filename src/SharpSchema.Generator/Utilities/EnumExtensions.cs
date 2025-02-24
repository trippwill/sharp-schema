using System.Runtime.CompilerServices;
using SharpSchema.Annotations;

namespace SharpSchema.Generator.Utilities;

internal static class EnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckFlag(this AccessibilityMode value, AccessibilityMode flag) => (value & flag) == flag;

    public static bool CheckFlag(this TraversalMode value, TraversalMode flag) => (value & flag) == flag;
}
