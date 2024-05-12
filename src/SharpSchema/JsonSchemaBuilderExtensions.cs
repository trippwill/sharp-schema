// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Humanizer;
using Json.More;
using Json.Schema;
using Microsoft;
using SharpSchema.Annotations;
using SharpSchema.TypeHandlers;

namespace SharpSchema;

/// <summary>
/// Extension methods for building JSON schemas.
/// </summary>
public static class JsonSchemaBuilderExtensions
{
    private static readonly CachingTypeHandler CachingTypeHandler = new();

    /// <summary>
    /// Converts a <see cref="Type"/> to a JSON schema.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="isRootType">Indicates whether the current type is the top-level type.</param>
    /// <param name="propertyAttributeData">The attribute data from the owning property.</param>
    /// <returns>The JSON schema represented by a <see cref="JsonSchemaBuilder"/>.</returns>
    public static JsonSchemaBuilder AddType(this JsonSchemaBuilder builder, Type type, ConverterContext context, bool isRootType = true, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        Requires.NotNull(builder, nameof(builder));
        Requires.NotNull(type, nameof(type));
        Requires.NotNull(context, nameof(context));

        if (++context.CurrentDepth > context.MaxDepth)
        {
            throw new InvalidOperationException($"Exceeded object graph depth of {context.MaxDepth}.");
        }

        try
        {
            TypeHandler.Result result = CachingTypeHandler.TryHandle(builder, context, type, isRootType, propertyAttributeData);
            if (result.ResultKind == TypeHandler.ResultKind.Handled)
            {
                return result.Builder;
            }

            return Assumes.NotReachable<JsonSchemaBuilder>();
        }
        finally
        {
            context.CurrentDepth--;
        }
    }

    /// <summary>
    /// Adds property information to the JSON schema builder.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="property">The <see cref="PropertyInfo"/> representing the property.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="isRequired">Indicates whether the property is required.</param>
    /// <returns>The updated JSON schema builder.</returns>
    public static JsonSchemaBuilder AddPropertyInfo(this JsonSchemaBuilder builder, PropertyInfo property, ConverterContext context, out bool isRequired)
    {
        isRequired = false;

        if (property.IsRequired(out bool isNullable))
        {
            isRequired = true;
        }

        if (property.TryGetCustomAttributeData(typeof(SchemaConstAttribute), out CustomAttributeData? cad) &&
            cad.TryGetConstructorArgument(0, out object? value))
        {
            isRequired = true;
            builder = builder.Const(JsonSerializer.SerializeToNode(value));
        }
        else if (isNullable)
        {
            builder = builder
                .OneOf(
                    new JsonSchemaBuilder()
                        .AddType(
                            property.PropertyType,
                            context,
                            isRootType: false,
                            propertyAttributeData: property.GetCustomAttributesData())
                        .AddPropertyConstraints(property),
                    new JsonSchemaBuilder()
                        .Type(SchemaValueType.Null));
        }
        else
        {
            builder = builder
                .AddType(
                    property.PropertyType,
                    context,
                    isRootType: false,
                    propertyAttributeData: property.GetCustomAttributesData())
                .AddPropertyConstraints(property);
        }

        return builder
            .Title(property.Name.Titleize())
            .AddPropertyAnnotations(property);
    }

    /// <summary>
    /// Adds an array type to the JSON schema builder.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="itemType">The type of the array items.</param>
    /// <param name="context">The converter context.</param>
    /// <returns>The updated JSON schema builder.</returns>
    internal static JsonSchemaBuilder AddArrayType(this JsonSchemaBuilder builder, Type itemType, ConverterContext context)
    {
        JsonSchemaBuilder itemSchema = new JsonSchemaBuilder()
            .AddType(itemType, context, isRootType: false);

        return builder
            .Comment($"{itemType.Name}[]")
            .Type(SchemaValueType.Array)
            .Items(itemSchema)
            .AdditionalItems(false);
    }

    /// <summary>
    /// Adds type annotations to the JSON schema builder based on the provided <see cref="Type"/>.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="type">The <see cref="Type"/> to add annotations for.</param>
    /// <returns>The updated JSON schema builder.</returns>
    internal static JsonSchemaBuilder AddTypeAnnotations(this JsonSchemaBuilder builder, Type type)
    {
        return builder.AddCommonAnnotations(type, type.Name);
    }

    /// <summary>
    /// Adds type annotations to the JSON schema builder based on the provided <see cref="PropertyInfo"/>.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="property">The <see cref="PropertyInfo"/> to add annotations for.</param>
    /// <returns>The updated JSON schema builder.</returns>
    private static JsonSchemaBuilder AddPropertyAnnotations(this JsonSchemaBuilder builder, PropertyInfo property)
    {
        return builder.AddCommonAnnotations(property);
    }

    private static JsonSchemaBuilder AddCommonAnnotations(this JsonSchemaBuilder builder, MemberInfo info, string? commentFallback = null)
    {
        if (info.TryGetCustomAttributeData<SchemaMetaAttribute>(out CustomAttributeData? cad))
        {
            if (cad.TryGetNamedArgument(nameof(SchemaMetaAttribute.Title), out string? title))
            {
                builder = builder.Title(title);
            }

            if (cad.TryGetNamedArgument(nameof(SchemaMetaAttribute.Description), out string? description))
            {
                builder = builder.Description(description);
            }

            if (cad.TryGetNamedArgument(nameof(SchemaMetaAttribute.Comment), out string? commentValue))
            {
                builder = builder.Comment(commentValue);
            }
            else if (commentFallback is not null)
            {
                builder = builder.Comment(commentFallback);
            }
        }

        return builder;
    }

    private static JsonSchemaBuilder AddPropertyConstraints(this JsonSchemaBuilder builder, PropertyInfo property)
    {
        Type type = property.PropertyType;
        if (type.IsGenericType && type.GetGenericTypeDefinition().Name == typeof(Nullable<>).Name)
        {
            type = type.GetGenericArguments().FirstOrDefault() ?? Assumes.NotReachable<Type>();
        }

        // if the property type is a number
        if (type.IsNumber())
        {
            // if the property has a value range attribute, set the minimum and maximum values
            if (property.TryGetCustomAttributeData<SchemaValueRangeAttribute>(out CustomAttributeData? rangeCad))
            {
                if (rangeCad.TryGetNamedArgument(nameof(SchemaValueRangeAttribute.Min), out double min))
                {
                    builder = builder.Minimum(decimal.CreateSaturating(min));
                }

                if (rangeCad.TryGetNamedArgument(nameof(SchemaValueRangeAttribute.Max), out double max))
                {
                    builder = builder.Maximum(decimal.CreateSaturating(max));
                }
            }
        }

        // if the property is a string
        if (type.Name == typeof(string).Name)
        {
            // if the property has a format attribute, set the format
            if (property.TryGetCustomAttributeData<SchemaFormatAttribute>(out CustomAttributeData? formatCad))
            {
                string? format = formatCad.GetConstructorArgument<string>(0);
                if (format is not null)
                {
                    builder = builder.Format(format);
                }
            }

            // if the property has a regex attribute, set the pattern
            if (property.TryGetCustomAttributeData<SchemaRegexAttribute>(out CustomAttributeData? regexCad))
            {
                if (regexCad.TryGetNamedArgument(nameof(SchemaRegexAttribute.ApplyToPropertyName), out bool? applyToPropertyName))
                {
                    if (!applyToPropertyName.Value)
                    {
                        string? pattern = regexCad.GetConstructorArgument<string>(0);
                        if (pattern is not null)
                        {
                            builder = builder.Pattern(pattern);
                        }
                    }
                }
            }

            // if the property has a string length attribute, set the minimum and maximum lengths
            if (property.TryGetCustomAttributeData<SchemaLengthRangeAttribute>(out CustomAttributeData? lengthCad))
            {
                if (lengthCad.TryGetNamedArgument(nameof(SchemaLengthRangeAttribute.Min), out uint min))
                {
                    builder = builder.MinLength(min);
                }

                if (lengthCad.TryGetNamedArgument(nameof(SchemaLengthRangeAttribute.Max), out uint max))
                {
                    builder = builder.MaxLength(max);
                }
            }

            return builder;
        }

        if (!property.PropertyType.IsValueType)
        {
            // if the property has a properties range attribute, set the minimum and maximum properties
            if (property.TryGetCustomAttributeData<SchemaPropertiesRangeAttribute>(out CustomAttributeData? rangeCad))
            {
                if (rangeCad.TryGetNamedArgument(nameof(SchemaPropertiesRangeAttribute.Min), out uint min))
                {
                    builder = builder.MinProperties(min);
                }

                if (rangeCad.TryGetNamedArgument(nameof(SchemaPropertiesRangeAttribute.Max), out uint max))
                {
                    builder = builder.MaxProperties(max);
                }
            }
        }

        return builder;
    }
}
