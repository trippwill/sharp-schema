// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using SharpMeta;
using SharpSchema.Annotations;

namespace SharpSchema;

/// <summary>
/// Information about a schema root type.
/// </summary>
public record struct RootTypeContext(Type Type, string? Filename, string? Id, string? CommonNamespace)
{
    /// <summary>
    /// Creates a <see cref="RootTypeContext"/> instance from the specified <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="T">The type to create the <see cref="RootTypeContext"/> from.</typeparam>
    /// <returns>A new instance of <see cref="RootTypeContext"/>.</returns>
    public static RootTypeContext FromType<T>() => FromType(typeof(T));

    /// <summary>
    /// Creates a <see cref="RootTypeContext"/> instance from the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to create the <see cref="RootTypeContext"/> from.</param>
    /// <returns>A new instance of <see cref="RootTypeContext"/>.</returns>
    public static RootTypeContext FromType(Type type)
    {
        if (type.TryGetCustomAttributeData(typeof(SchemaRootAttribute), out CustomAttributeData? cad))
        {
            return new(
                type,
                Filename: cad.GetNamedArgument<string>("Filename"),
                Id: cad.GetNamedArgument<string>("Id"),
                CommonNamespace: cad.GetNamedArgument<string>("CommonNamespace"));
        }

        return new(type, null, null, null);
    }
}
