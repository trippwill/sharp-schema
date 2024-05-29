﻿// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Json.Schema;
using SharpSchema.Annotations;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles enumerable types.
/// </summary>
internal class EnumerableTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        if (!type.IsGenericType)
        {
            return Result.NotHandled(builder);
        }

        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        Type[] genericArguments = type.GetGenericArguments();

        if (!genericTypeDefinition.ImplementsAnyInterface((typeof(IEnumerable).Namespace, typeof(IEnumerable).Name)))
        {
            return Result.NotHandled(builder);
        }

        Type elementType = genericArguments.Single();

        try
        {
            builder = builder
                .AddTypeAnnotations(type)
                .AddArrayType(elementType, context, propertyAttributeData);

            return Result.Handled(builder);
        }
        catch (Exception ex)
        {
            return Result.Fault(builder, ex.Message);
        }
    }
}
