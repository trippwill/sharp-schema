using System.Runtime.CompilerServices;

namespace SharpSchema.Generator.Utilities;

internal static class EnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckFlag(this AllowedAccessibilities value, AllowedAccessibilities flag) => (value & flag) == flag;
}
