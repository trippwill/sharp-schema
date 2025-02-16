using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace SharpSchema.Generator.Utilities;

[ExcludeFromCodeCoverage]
internal readonly struct AttributeHandler(AttributeData? data)
{
    public object? this[int index]
    {
        get
        {
            if (data is null || index >= data.ConstructorArguments.Length)
            {
                return null;
            }

            TypedConstant argument = data.ConstructorArguments[index];
            return argument.Kind switch
            {
                TypedConstantKind.Primitive or TypedConstantKind.Enum => argument.Value,
                TypedConstantKind.Array => argument.Values.Select(v => v.Value).ToArray(),
                _ => null
            };
        }
    }

    public object? this[string name]
    {
        get
        {
            if (data is null)
            {
                return null;
            }

            TypedConstant argument = data.NamedArguments.FirstOrDefault(a => a.Key == name).Value;
            return argument.Kind switch
            {
                TypedConstantKind.Primitive or TypedConstantKind.Enum => argument.Value,
                TypedConstantKind.Array => argument.Values.Select(v => v.Value).ToArray(),
                _ => null
            };
        }
    }

    public ImmutableArray<T>? GetArray<T>(int index)
        where T : notnull
    {
        if (data is null || index >= data.ConstructorArguments.Length)
        {
            return null;
        }

        TypedConstant argument = data.ConstructorArguments[index];
        return argument.Kind == TypedConstantKind.Array
            ? [.. argument.Values
                    .Select(v => v.Value)
                    .OfType<T>()]
            : null;
    }

    public ImmutableArray<T>? GetArray<T>(string name)
        where T : notnull
    {
        if (data is null)
            return null;

        TypedConstant argument = data.NamedArguments.FirstOrDefault(a => a.Key == name).Value;
        return argument.Kind == TypedConstantKind.Array
            ? [.. argument.Values
                    .Select(v => v.Value)
                    .OfType<T>()]
            : null;
    }
}
