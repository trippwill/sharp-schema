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

    public T? Get<T>(int index)
        where T : struct
    {
        if (data is null || index >= data.ConstructorArguments.Length)
        {
            return default;
        }

        TypedConstant argument = data.ConstructorArguments[index];
        return argument.Kind switch
        {
            TypedConstantKind.Primitive => argument.Value is T value ? value : default,
            TypedConstantKind.Enum => (T)argument.Value!,
            _ => default
        };
    }

    public string? Get(int index)
    {
        if (data is null || index >= data.ConstructorArguments.Length)
        {
            return default;
        }

        TypedConstant argument = data.ConstructorArguments[index];
        return argument.Kind switch
        {
            TypedConstantKind.Primitive => argument.Value is string value ? value : default,
            _ => default
        };
    }

    public T? Get<T>(string name)
        where T : struct
    {
        if (data is null)
        {
            return default;
        }
        TypedConstant argument = data.NamedArguments.FirstOrDefault(a => a.Key == name).Value;
        return argument.Kind switch
        {
            TypedConstantKind.Primitive => argument.Value is T value ? value : default,
            TypedConstantKind.Enum => (T)argument.Value!,
            _ => default
        };
    }

    public string? Get(string name)
    {
        if (data is null)
        {
            return default;
        }

        TypedConstant argument = data.NamedArguments.FirstOrDefault(a => a.Key == name).Value;
        return argument.Kind switch
        {
            TypedConstantKind.Primitive => argument.Value is string value ? value : default,
            _ => default
        };
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
