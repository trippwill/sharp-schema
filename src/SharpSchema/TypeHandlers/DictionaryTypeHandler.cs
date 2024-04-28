// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Json.Schema;
using Microsoft;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles dictionary types.
/// </summary>
internal class DictionaryTypeHandler : TypeHandler
{
    private static readonly (string? Namespace, string Name)[] OpenDictionaryGenericTypeNames =
    [
        (typeof(IDictionary<,>).GetGenericTypeDefinition().Namespace, typeof(IDictionary<,>).GetGenericTypeDefinition().Name),
        (typeof(IReadOnlyDictionary<,>).GetGenericTypeDefinition().Namespace, typeof(IReadOnlyDictionary<,>).GetGenericTypeDefinition().Name),
        (typeof(IImmutableDictionary<,>).GetGenericTypeDefinition().Namespace, typeof(IImmutableDictionary<,>).GetGenericTypeDefinition().Name),
        (typeof(Dictionary<,>).GetGenericTypeDefinition().Namespace, typeof(Dictionary<,>).GetGenericTypeDefinition().Name),
        (typeof(ImmutableDictionary<,>).GetGenericTypeDefinition().Namespace, typeof(ImmutableDictionary<,>).GetGenericTypeDefinition().Name),
    ];

    /// <inheritdoc/>
    public override Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        if (!type.IsGenericType)
        {
            return Result.NotHandled(builder);
        }

        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        Type[] genericArguments = type.GetGenericArguments();

        if (!genericTypeDefinition.ImplementsAnyInterface(OpenDictionaryGenericTypeNames))
        {
            return Result.NotHandled(builder);
        }

        Requires.Range(genericArguments.Length <= 2, nameof(type), "Only dictionaries with up to two generic arguments are supported.");

        // assume that dictionaries with up to one generic argument have string keys
        Type keyType = genericArguments.Length <= 1
            ? typeof(string)
            : genericArguments[0];

        Type? valueType = genericArguments.Length <= 1
            ? genericArguments.FirstOrDefault()
            : genericArguments[1];

        Requires.Argument(keyType.Name == typeof(string).Name, nameof(type), "Only dictionaries with string keys are supported.");

        JsonSchemaBuilder keySchema = new JsonSchemaBuilder()
            .AddType(keyType, context, isRootType = false);

        // if the property has a regular expression attribute, use that as the key pattern
        if (propertyAttributeData is not null)
        {
            CustomAttributeData? regexAttribute = propertyAttributeData.FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.RegularExpressionAttribute");
            if (regexAttribute is not null && regexAttribute.ConstructorArguments.Count == 1)
            {
                keySchema.Pattern((string)regexAttribute.ConstructorArguments[0].Value!);
            }
        }

        builder = valueType is not null
            ? builder
                .Comment($"[{keyType.Name} = {valueType.Name}]")
                .Type(SchemaValueType.Object)
                .PropertyNames(keySchema)
                .AdditionalProperties(new JsonSchemaBuilder()
                    .AddType(valueType, context, isRootType: false))
            : builder
                .Comment($"[{keyType.Name}]")
                .Type(SchemaValueType.Object)
                .PropertyNames(keySchema);

        return Result.Handled(builder);
    }
}
