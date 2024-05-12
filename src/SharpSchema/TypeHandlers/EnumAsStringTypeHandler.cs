﻿// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using Humanizer;
using Json.Schema;
using SharpSchema.Annotations;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles enum types by representing them as strings.
/// </summary>
internal class EnumAsStringTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        if (!type.IsEnum || context.EnumAsUnderlyingType)
        {
            return Result.NotHandled(builder);
        }

        string[] enumNames = Enum.GetNames(type);
        ImmutableArray<string>.Builder kebabCaseEnumNames = ImmutableArray.CreateBuilder<string>(enumNames.Length);
        for (int i = 0; i < enumNames.Length; i++)
        {
            if (type.GetField(enumNames[i]) is FieldInfo fieldInfo)
            {
                if (fieldInfo.TryGetCustomAttributeData<SchemaIgnoreAttribute>(out _))
                {
                    continue;
                }

                if (fieldInfo.TryGetCustomAttributeData<SchemaEnumValueAttribute>(out CustomAttributeData? attribute) &&
                attribute.TryGetConstructorArgument(0, out string? value))
                {
                    kebabCaseEnumNames.Add(value);
                    continue;
                }
            }

            kebabCaseEnumNames.Add(enumNames[i].Kebaberize());
        }

        builder = builder
            .Comment(type.Name)
            .Type(SchemaValueType.String)
            .Enum(kebabCaseEnumNames.ToImmutable());

        return Result.Handled(builder);
    }
}
