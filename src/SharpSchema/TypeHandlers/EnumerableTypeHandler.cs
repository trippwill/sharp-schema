// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Json.Schema;
using libanvl;
using SharpMeta;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles enumerable types.
/// </summary>
internal class EnumerableTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(
        JsonSchemaBuilder builder,
        ConverterContext context,
        Type type,
        bool isRootType,
        Opt<PropertyInfo> owningProperty)
    {
        if (!type.IsGenericType && !type.IsArray)
        {
            return Result.NotHandled(builder);
        }

        Type? elementType;

        if (type.IsGenericType)
        {
            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            Type[] genericArguments = type.GetGenericArguments();

            if (!genericTypeDefinition.ImplementsAnyInterface(typeof(IEnumerable).FullName))
            {
                return Result.NotHandled(builder);
            }

            elementType = genericArguments.Single();
        }
        else
        {
            elementType = type.GetElementType();
        }

        if (elementType is null)
        {
            return Result.NotHandled(builder);
        }

        try
        {
            builder = builder
                .AddTypeAnnotations(context, type, disallowDocComments: true)
                .AddArrayType(context, elementType, owningProperty);

            return Result.Handled(builder);
        }
        catch (Exception ex)
        {
            return Result.Fault(builder, ex.Message);
        }
    }
}
