using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace SharpSchema.Generator.Utilities;

internal static class AttributeDataExtensions
{
    public static T? GetNamedArgument<T>(
        this AttributeData attributeData,
        string argumentName,
        StringComparison comparisonType = StringComparison.Ordinal)
        where T : notnull
    {
        TypedConstant namedArgument = attributeData
            .NamedArguments.FirstOrDefault(a => a.Key.Equals(argumentName, comparisonType))
            .Value;

        return namedArgument.Kind switch
        {
            TypedConstantKind.Primitive or TypedConstantKind.Enum => namedArgument.Value is T value ? value : default,
            TypedConstantKind.Array => throw new InvalidOperationException($"Cannot get named argument '{argumentName}' as a single value."),
            _ => default
        };
    }

    public static List<T> GetNamedArgumentArray<T>(
        this AttributeData attributeData,
        string argumentName,
        StringComparison comparisonType = StringComparison.Ordinal)
        where T : notnull
    {
        TypedConstant namedArgument = attributeData
            .NamedArguments.FirstOrDefault(a => a.Key.Equals(argumentName, comparisonType))
            .Value;

        List<T> result = [];
        if (namedArgument.Kind == TypedConstantKind.Array)
        {
            foreach (TypedConstant value in namedArgument.Values)
            {
                if (value.Value is T item)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        throw new InvalidOperationException($"Cannot get named argument '{argumentName}' as an array.");
    }

    public static T? GetConstructorArgument<T>(this AttributeData attributeData, int argumentIndex)
        where T : notnull
    {
        if (argumentIndex >= attributeData.ConstructorArguments.Length)
        {
            return default;
        }

        TypedConstant constructorArgument = attributeData.ConstructorArguments[argumentIndex];

        return constructorArgument.Kind switch
        {
            TypedConstantKind.Primitive or TypedConstantKind.Enum => constructorArgument.Value is T value ? value : default,
            TypedConstantKind.Array => throw new InvalidOperationException($"Cannot get constructor argument '{argumentIndex}' as a single value."),
            _ => default
        };
    }

    public static List<T> GetConstructorArgumentArray<T>(this AttributeData attributeData, int argumentIndex)
        where T : notnull
    {
        if (argumentIndex >= attributeData.ConstructorArguments.Length)
        {
            return [];
        }

        TypedConstant constructorArgument = attributeData.ConstructorArguments[argumentIndex];

        List<T> result = [];
        if (constructorArgument.Kind == TypedConstantKind.Array)
        {
            foreach (TypedConstant value in constructorArgument.Values)
            {
                if (value.Value is T item)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        throw new InvalidOperationException($"Cannot get constructor argument '{argumentIndex}' as an array.");
    }
}
