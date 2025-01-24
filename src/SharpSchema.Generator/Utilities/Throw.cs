using System.Diagnostics.CodeAnalysis;

namespace SharpSchema.Generator.Utilities;

internal static class Throw
{
    public static void IfNullArgument<T>(T? value, string paramName) where T : class
    {
        if (value is null) throw new ArgumentNullException(paramName);
    }

    [ExcludeFromCodeCoverage]
    public static T ForNullValue<T>(string message) where T : notnull
    {
        throw new InvalidOperationException(message);
    }
}
