// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Humanizer;
using Json.More;
using Json.Schema;
using Microsoft;
using SharpSchema.TypeHandlers;

namespace SharpSchema;

/// <summary>
/// Extension methods for building JSON schemas.
/// </summary>
public static class JsonSchemaBuilderExtensions
{
    private static readonly TypeHandler[] TypeHandlers =
    [
        new NullableValueTypeHandler(),
        new AmbientValueTypeHandler(),
        new StringFormatTypeHandler(),
        new EnumAsStringTypeHandler(),
        new TypeCodeTypeHandler(),
        new ArrayTypeHandler(),
        new DictionaryTypeHandler(),
        new EnumerableTypeHandler(),
        new FallbackTypeHandler(),
    ];

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
            foreach (TypeHandler typeHandler in TypeHandlers)
            {
                TypeHandler.Result result = typeHandler.TryHandle(builder, context, type, isRootType, propertyAttributeData);
                (builder, bool isHandled) = result.Unwrap();

                if (isHandled)
                {
                    return builder;
                }
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

        if (isNullable)
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
        JsonSchemaBuilder itemSchema = new JsonSchemaBuilder().AddType(itemType, context, isRootType: false);

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
        IList<CustomAttributeData> customAttributeData = type.GetCustomAttributesData();
        return builder.AddCommonAnnotations(customAttributeData, type.Name);
    }

    /// <summary>
    /// Adds type annotations to the JSON schema builder based on the provided <see cref="PropertyInfo"/>.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="property">The <see cref="PropertyInfo"/> to add annotations for.</param>
    /// <returns>The updated JSON schema builder.</returns>
    private static JsonSchemaBuilder AddPropertyAnnotations(this JsonSchemaBuilder builder, PropertyInfo property)
    {
        IList<CustomAttributeData> customAttributeData = property.GetCustomAttributesData();
        return builder.AddCommonAnnotations(customAttributeData);
    }

    private static JsonSchemaBuilder AddCommonAnnotations(this JsonSchemaBuilder builder, IList<CustomAttributeData> customAttributeData, string? commentFallback = null)
    {
        if (customAttributeData
            .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DescriptionAttribute") is { ConstructorArguments: { Count: 1 } descriptionArguments })
        {
            string description = (string)descriptionArguments[0].Value!;
            builder = builder.Comment(description);
        }
        else if (commentFallback is not null)
        {
            builder = builder.Comment(commentFallback);
        }

        // if the type has a DisplayAttribute, add it to the schema
        if (customAttributeData
            .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.DisplayAttribute") is { NamedArguments: { Count: > 0 } displayArguments })
        {
            CustomAttributeNamedArgument nameArgument = displayArguments.FirstOrDefault(descriptionData => descriptionData.MemberName == "Name");
            if (nameArgument != default)
            {
                string name = (string)nameArgument.TypedValue.Value!;
                builder = builder.Title(name);
            }

            CustomAttributeNamedArgument descriptionArgument = displayArguments.FirstOrDefault(descriptionData => descriptionData.MemberName == "Description");
            if (descriptionArgument != default)
            {
                string description = (string)descriptionArgument.TypedValue.Value!;
                builder = builder.Description(description);
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

        // if the property type is a number and has a range, set the minimum and maximum values
        if (type.IsNumber()
            && property.GetCustomAttributesData()
                .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.RangeAttribute") is { ConstructorArguments: { Count: 2 } rangeArguments })
        {
            if (rangeArguments[0].Value is int minInt)
            {
                builder = builder
                    .Minimum(minInt)
                    .Maximum((int)rangeArguments[1].Value!);
            }
            else if (rangeArguments[0].Value is double minDouble)
            {
                builder = builder
                    .Minimum(decimal.CreateSaturating(minDouble))
                    .Maximum(decimal.CreateSaturating((double)rangeArguments[1].Value!));
            }
            else
            {
                Assumes.NotReachable();
            }

            return builder;
        }

        // if the property is a string
        if (type.Name == typeof(string).Name)
        {
            // if the property has a regular expression attribute, set the pattern
            if (property.GetCustomAttributesData()
                .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.RegularExpressionAttribute") is { ConstructorArguments: { Count: 1 } regexArguments })
            {
                builder = builder.Pattern((string)regexArguments[0].Value!);
            }

            // if the property has a string length attribute, set the minimum and maximum lengths
            if (property.GetCustomAttributesData()
                .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.StringLengthAttribute") is { ConstructorArguments: { Count: 1 } lengthArguments })
            {
                if (lengthArguments[0].Value is uint length)
                {
                    builder = builder
                        .MinLength(length)
                        .MaxLength(length);
                }
                else
                {
                    Assumes.NotReachable();
                }
            }

            // if the property has a max length attribute, set the maximum length
            if (property.GetCustomAttributesData()
                .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.MaxLengthAttribute") is { ConstructorArguments: { Count: 1 } maxLengthArguments })
            {
                if (maxLengthArguments[0].Value is uint maxLength)
                {
                    builder = builder.MaxLength(maxLength);
                }
                else
                {
                    Assumes.NotReachable();
                }
            }

            // if the property has a min length attribute, set the minimum length
            if (property.GetCustomAttributesData()
                .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.MinLengthAttribute") is { ConstructorArguments: { Count: 1 } minLengthArguments })
            {
                if (minLengthArguments[0].Value is uint minLength)
                {
                    builder = builder.MinLength(minLength);
                }
                else
                {
                    Assumes.NotReachable();
                }
            }

            return builder;
        }

        if (!property.PropertyType.IsValueType)
        {
            // if the property has a min length attribute, set the minimum properties
            if (property.GetCustomAttributesData()
                .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.MinLengthAttribute") is { ConstructorArguments: { Count: 1 } minLengthArguments })
            {
                if (minLengthArguments[0].Value is uint minLength)
                {
                    builder = builder.MinProperties(minLength);
                }
                else
                {
                    Assumes.NotReachable();
                }
            }
        }

        return builder;
    }
}
