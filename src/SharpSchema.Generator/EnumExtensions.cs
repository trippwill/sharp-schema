using System.Runtime.CompilerServices;

namespace SharpSchema.Generator;

internal static class EnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckFlag(this AllowedTypeDeclarations value, AllowedTypeDeclarations flag) => (value & flag) == flag;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckFlag(this AllowedAccessibilities value, AllowedAccessibilities flag) => (value & flag) == flag;
}
