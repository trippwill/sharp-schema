// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using libanvl;
using Microsoft;
using SharpMeta;
using SharpSchema.Annotations;
using SharpSchema.TypeHandlers;

namespace SharpSchema;

/// <summary>
/// Extension methods for building JSON schemas.
/// </summary>
public static class JsonSchemaBuilderExtensions
{
    private static readonly AggregateTypeHandler AggregateTypeHandler = new();

    /// <summary>
    /// Adds type information to the JSON schema builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="rootTypeContext">The <see cref="Type"/> to convert.</param>
    /// <returns>The JSON schema represented by a <see cref="JsonSchemaBuilder"/>.</returns>
    public static JsonSchemaBuilder AddType(this JsonSchemaBuilder builder, ConverterContext context, RootTypeContext rootTypeContext)
    {
        return builder.AddType(context, rootTypeContext.Type, isRootType: true, owningProperty: null);
    }

    /// <summary>
    /// Adds type information to the JSON schema builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <returns>The JSON schema represented by a <see cref="JsonSchemaBuilder"/>.</returns>
    public static JsonSchemaBuilder AddType(this JsonSchemaBuilder builder, ConverterContext context, Type type)
    {
        return builder.AddType(context, type, isRootType: false, owningProperty: null);
    }

    /// <summary>
    /// Adds type information to the JSON schema builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <param name="owningProperty">The owning property.</param>
    /// <returns>The JSON schema represented by a <see cref="JsonSchemaBuilder"/>.</returns>
    public static JsonSchemaBuilder AddType(this JsonSchemaBuilder builder, ConverterContext context, Type type, PropertyInfo owningProperty)
    {
        return builder.AddType(context, type, isRootType: false, owningProperty: owningProperty);
    }

    /// <summary>
    /// Adds type information to the JSON schema builder based on the provided property.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="property">The property information.</param>
    /// <returns>The updated JSON schema builder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the property is null.</exception>
    public static JsonSchemaBuilder AddType(this JsonSchemaBuilder builder, ConverterContext context, PropertyInfo property)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));

        return builder.AddType(context, property.PropertyType, isRootType: false, owningProperty: property);
    }

    /// <summary>
    /// Adds property information to the JSON schema builder.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="property">The <see cref="PropertyInfo"/> representing the property.</param>
    /// <param name="isRequired">Indicates whether the property is required.</param>
    /// <returns>The updated JSON schema builder.</returns>
    public static JsonSchemaBuilder AddPropertyInfo(this JsonSchemaBuilder builder, ConverterContext context, PropertyInfo property, out bool isRequired)
    {
        ArgumentNullException.ThrowIfNull(property, nameof(property));
        ArgumentNullException.ThrowIfNull(context, nameof(context));

        isRequired = false;

        if (property.IsRequired(out bool isNullable))
        {
            isRequired = true;
        }

        Opt<JsonNode> constNode = Opt.From(property.GetCustomAttributeData<SchemaConstAttribute>())
            .Select(cad => cad.GetConstructorArgument<object>(0))
            .Select(value => JsonSerializer.SerializeToNode(value));

        if (constNode)
        {
            isRequired = true;
            builder = builder.Const(constNode);
        }
        else if (isNullable)
        {
            builder = builder
                .OneOf(
                    new JsonSchemaBuilder()
                        .AddType(
                            context,
                            property)
                        .AddPropertyConstraints(property),
                    new JsonSchemaBuilder()
                        .Type(SchemaValueType.Null));
        }
        else
        {
            builder = builder
                .AddType(
                    context,
                    property)
                .AddPropertyConstraints(property);
        }

        return builder
            .AddPropertyAnnotations(context, property);
    }

    /// <summary>
    /// Adds type information to the JSON schema builder.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <param name="isRootType">Indicates whether the current type is the top-level type.</param>
    /// <param name="owningProperty">The owning property.</param>
    /// <returns>The JSON schema represented by a <see cref="JsonSchemaBuilder"/>.</returns>
    internal static JsonSchemaBuilder AddType(this JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType, PropertyInfo? owningProperty)
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
            TypeHandler.Result result = AggregateTypeHandler.TryHandle(
                builder,
                context,
                type,
                isRootType,
                owningProperty);

            return result.ResultKind switch
            {
                TypeHandler.ResultKind.Fault => throw new InvalidOperationException(string.Join(Environment.NewLine, result.Messages)),
                TypeHandler.ResultKind.Handled => result.Builder,
                _ => Assumes.NotReachable<JsonSchemaBuilder>(),
            };
        }
        finally
        {
            context.CurrentDepth--;
        }
    }

    /// <summary>
    /// Adds an array type to the JSON schema builder.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="itemType">The type of the array items.</param>
    /// <param name="owningProperty">The owning property.</param>
    /// <returns>The updated JSON schema builder.</returns>
    internal static JsonSchemaBuilder AddArrayType(this JsonSchemaBuilder builder, ConverterContext context, Type itemType, Opt<PropertyInfo> owningProperty)
    {
        JsonSchemaBuilder itemSchema = new JsonSchemaBuilder()
            .AddType(context: context, type: itemType);

        builder = builder
            .Comment($"{itemType.Name}[]")
            .Type(SchemaValueType.Array)
            .Items(itemSchema);

        Opt<CustomAttributeData> rangeAttribute = owningProperty
            .Select(pi => pi.GetCustomAttributeData<SchemaItemsRangeAttribute>());

        builder = rangeAttribute
            .Select(cad => cad.GetNamedArgument<uint?>(nameof(SchemaItemsRangeAttribute.Min)))
            .Match(
                min => builder.MinItems(min),
                () => builder);

        builder = rangeAttribute
            .Select(cad => cad.GetNamedArgument<uint?>(nameof(SchemaItemsRangeAttribute.Max)))
            .Match(
                max => builder.MaxItems(max),
                () => builder);

        builder = rangeAttribute
            .Select(cad => cad.GetNamedArgument<bool?>(nameof(SchemaItemsRangeAttribute.UniqueItems)))
            .Match(
                uniqueItems => builder.UniqueItems(uniqueItems),
                () => builder);

        return builder;
    }

    /// <summary>
    /// Adds type annotations to the JSON schema builder based on the provided <see cref="Type"/>.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="type">The <see cref="Type"/> to add annotations for.</param>
    /// <param name="disallowDocComments">Whether to suppress doc comments for the type.</param>
    /// <returns>The updated JSON schema builder.</returns>
    internal static JsonSchemaBuilder AddTypeAnnotations(this JsonSchemaBuilder builder, ConverterContext context, Type type, bool disallowDocComments)
    {
        // only parse doc comments for types in the root type assembly
        bool parseDocComments = !disallowDocComments && context.ParseDocComments && (type.Assembly.GetName().FullName == context.RootTypeAssemblyName.FullName);
        return builder.AddMetaAnnotations(type, parseDocComments);
    }

    /// <summary>
    /// Adds type annotations to the JSON schema builder based on the provided <see cref="PropertyInfo"/>.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="property">The <see cref="PropertyInfo"/> to add annotations for.</param>
    /// <returns>The updated JSON schema builder.</returns>
    private static JsonSchemaBuilder AddPropertyAnnotations(this JsonSchemaBuilder builder, ConverterContext context, PropertyInfo property)
    {
        return builder.AddMetaAnnotations(property, context.ParseDocComments);
    }

    private static JsonSchemaBuilder AddMetaAnnotations(this JsonSchemaBuilder builder, MemberInfo info, bool parseDocComments)
    {
        Opt<CustomAttributeData> meta = info.GetCustomAttributeData<SchemaMetaAttribute>();
        Opt<DocComments> docComments = parseDocComments
            ? info.GetDocComments()?.NormalizeForSchema()
            : Opt<DocComments>.None;

        builder = meta
            .Select(cad => cad.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Title)))
            .OrThen(docComments.Select(info => info.Summary.ParseDocString()))
            .Match(
                title => builder.Title(title),
                () => builder);

        builder = meta
            .Select(cad => cad.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Description)))
            .OrThen(docComments.Select(info => info.Remarks.ParseDocString()))
            .Match(
                description => builder.Description(description),
                () => builder);

        builder = meta
            .Select(cad => cad.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Comment)))
            .Match(
                comment => builder.Comment(comment),
                () => builder);

        builder = docComments
            .Match(docComments =>
            {
                if (docComments.Examples.IsDefaultOrEmpty)
                    return builder;

                return builder.Examples(docComments.Examples.Select(s => JsonValue.Create(s)));
            },
            () => builder);
        return builder;
    }

    private static JsonSchemaBuilder AddPropertyConstraints(this JsonSchemaBuilder builder, PropertyInfo property)
    {
        Type type = property.PropertyType;
        if (type.TryUnwrapNullable(out Type? unwrapped))
        {
            type = unwrapped;
        }

        // if the property type is a number
        if (type.IsSchemaNumeric())
        {
            // if the property has a value range attribute, set the minimum and maximum values
            Opt<CustomAttributeData> rangeAttribute = property.GetCustomAttributeData<SchemaValueRangeAttribute>();
            builder = rangeAttribute
                .Select(cad => cad.GetNamedArgument<double?>(nameof(SchemaValueRangeAttribute.Min)))
                .Match(
                    min => builder.Minimum(decimal.CreateSaturating(min)),
                    () => builder);

            builder = rangeAttribute
                .Select(cad => cad.GetNamedArgument<double?>(nameof(SchemaValueRangeAttribute.Max)))
                .Match(
                    max => builder.Maximum(decimal.CreateSaturating(max)),
                    () => builder);
        }

        // if the property is a string
        if (type.Name == typeof(string).Name)
        {
            // if the property has a format attribute, set the format
            builder = Opt.From(property.GetCustomAttributeData<SchemaFormatAttribute>())
                .Select(cad => cad.GetConstructorArgument<string>(0))
                .Match(
                    format => builder.Format(format),
                    () => builder);

            // if the property has a regex attribute, set the pattern;
            Opt<CustomAttributeData> regexAttribute = property.GetCustomAttributeData<SchemaRegexAttribute>();
            if (regexAttribute)
            {
                Opt<bool> applyToPropertyName = regexAttribute.Select(cad => cad.GetNamedArgument<bool>(nameof(SchemaRegexAttribute.ApplyToPropertyName)));
                bool forPropertyName = applyToPropertyName
                    .Match(
                        value => value,
                        () => false);

                if (!forPropertyName)
                {
                    Opt<string> pattern = regexAttribute.Select(cad => cad.GetConstructorArgument<string>(0));
                    if (pattern)
                    {
                        builder = builder.Pattern(pattern.Unwrap());
                    }
                }
            }

            // if the property has a string length attribute, set the minimum and maximum lengths
            Opt<CustomAttributeData> lengthAttribute = property.GetCustomAttributeData<SchemaLengthRangeAttribute>();
            builder = lengthAttribute
                .Select(cad => cad.GetNamedArgument<uint?>(nameof(SchemaLengthRangeAttribute.Min)))
                .Match(
                    min => builder.MinLength(min),
                    () => builder);

            builder = lengthAttribute
                .Select(cad => cad.GetNamedArgument<uint?>(nameof(SchemaLengthRangeAttribute.Max)))
                .Match(
                    max => builder.MaxLength(max),
                    () => builder);

            return builder;
        }

        if (!property.PropertyType.IsValueType)
        {
            // if the property has a properties range attribute, set the minimum and maximum properties
            Opt<CustomAttributeData> rangeAttribute = property.GetCustomAttributeData<SchemaPropertiesRangeAttribute>();
            builder = rangeAttribute
                .Select(cad => cad.GetNamedArgument<uint?>(nameof(SchemaPropertiesRangeAttribute.Min)))
                .Match(
                    min => builder.MinProperties(min),
                    () => builder);

            builder = rangeAttribute
                .Select(cad => cad.GetNamedArgument<uint?>(nameof(SchemaPropertiesRangeAttribute.Max)))
                .Match(
                    max => builder.MaxProperties(max),
                    () => builder);
        }

        return builder;
    }
}
