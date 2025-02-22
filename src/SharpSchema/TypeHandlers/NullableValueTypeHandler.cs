﻿// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Json.Schema;
using libanvl;
using SharpMeta;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles nullable value types.
/// </summary>
internal class NullableValueTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(
        JsonSchemaBuilder builder,
        ConverterContext context,
        Type type,
        bool isRootType,
        Opt<PropertyInfo> propertyInfo)
    {
        if (!type.TryUnwrapNullable(out Type? underlyingType))
        {
            return Result.NotHandled(builder);
        }

        try
        {
            builder = builder.AddType(context, underlyingType, isRootType, propertyInfo);
            return Result.Handled(builder);
        }
        catch (Exception ex)
        {
            return Result.Fault(builder, ex.Message);
        }
    }
}
