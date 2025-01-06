﻿// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Json.Schema;
using libanvl;
using SharpMeta;
using SharpSchema.Annotations;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles dictionary types.
/// </summary>
internal class DictionaryTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(
        JsonSchemaBuilder builder,
        ConverterContext context,
        Type type,
        bool isRootType,
        Opt<PropertyInfo> propertyInfo)
    {
        if (!type.IsProbableDictionary(out Type? keyType, out Type? valueType))
        {
            return Result.NotHandled(builder);
        }

        if (keyType.Name != typeof(string).Name)
        {
            return Result.NotHandled(builder, "Only dictionaries with string keys are supported.");
        }

        JsonSchemaBuilder keySchema = new JsonSchemaBuilder()
            .AddType(context, keyType);

        // if the property has a regular expression attribute that applies to the property name, use that as the key pattern
        if (propertyInfo.IsSome && propertyInfo.Unwrap().GetAllCustomAttributeData(includeInherited: true) is IList<CustomAttributeData> cads)
        {
            if (cads.FirstOrDefault(cad => cad.AttributeType.FullName == typeof(SchemaRegexAttribute).FullName) is CustomAttributeData regexCad)
            {
                if (regexCad.TryGetNamedArgument(nameof(SchemaRegexAttribute.ApplyToPropertyName), out bool applyToPropertyName) && applyToPropertyName)
                {
                    if (regexCad.TryGetConstructorArgument(0, out string? pattern))
                    {
                        keySchema = keySchema.Pattern(pattern);
                    }
                }
            }
        }

        builder = valueType is not null
            ? builder
                .Comment($"[{keyType.Name} => {valueType.Name}]")
                .Type(SchemaValueType.Object)
                .PropertyNames(keySchema)
                .AdditionalProperties(new JsonSchemaBuilder()
                    .AddType(context: context, type: valueType))
            : builder
                .Comment($"[{keyType.Name}]")
                .Type(SchemaValueType.Object)
                .PropertyNames(keySchema);

        return Result.Handled(builder);
    }
}
