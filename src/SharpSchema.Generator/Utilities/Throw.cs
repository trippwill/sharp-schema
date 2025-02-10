using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SharpSchema.Generator.Utilities;

internal static class Throw
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void IfNullArgument<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        if (value is null) throw new ArgumentNullException(paramName);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [return: NotNullIfNotNull(nameof(value))]
    public static T ForUnexpectedNull<T>([NotNull] T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null, [CallerMemberName] string? caller = null)
        where T : class
    {
        if (value is null) throw new InvalidOperationException($"Unexpected null value {paramName} in {caller}");
        return value;
    }
}
