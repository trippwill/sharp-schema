using Microsoft.CodeAnalysis;

namespace SharpSchema.Generator.Utilities;

internal static class AttributeDataExtensions
{
    public static T? GetNamedArgument<T>(this AttributeData attributeData, string argumentName)
        where T : notnull
    {
        TypedConstant namedArgument = attributeData.NamedArguments.FirstOrDefault(a => a.Key == argumentName).Value;
        return namedArgument.Value is T value ? value : default;
    }

    public static T? GetConstructorArgument<T>(this AttributeData attributeData, int argumentIndex)
        where T : notnull
    {
        if (argumentIndex >= attributeData.ConstructorArguments.Length)
        {
            return default;
        }

        TypedConstant constructorArgument = attributeData.ConstructorArguments[argumentIndex];
        return constructorArgument.Value is T value ? value : default;
    }
}
