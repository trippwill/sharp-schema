// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Humanizer;
using Json.More;
using Json.Schema;
using Microsoft;

namespace SharpSchema;

/// <summary>
/// Extension methods for building JSON schemas.
/// </summary>
public static class JsonSchemaBuilderExtensions
{
    private static readonly (string? Namespace, string Name)[] OpenDictionaryGenericTypeNames =
    [
        (typeof(IDictionary<,>).GetGenericTypeDefinition().Namespace, typeof(IDictionary<,>).GetGenericTypeDefinition().Name),
        (typeof(IReadOnlyDictionary<,>).GetGenericTypeDefinition().Namespace, typeof(IReadOnlyDictionary<,>).GetGenericTypeDefinition().Name),
        (typeof(IImmutableDictionary<,>).GetGenericTypeDefinition().Namespace, typeof(IImmutableDictionary<,>).GetGenericTypeDefinition().Name),
        (typeof(Dictionary<,>).GetGenericTypeDefinition().Namespace, typeof(Dictionary<,>).GetGenericTypeDefinition().Name),
        (typeof(ImmutableDictionary<,>).GetGenericTypeDefinition().Namespace, typeof(ImmutableDictionary<,>).GetGenericTypeDefinition().Name),
    ];

    private static readonly Dictionary<string, Func<JsonSchemaBuilder, JsonSchemaBuilder>> StringFormatSchemas = new(capacity: 5)
    {
        [typeof(Guid).Name] = (builder) => builder
            .Comment("Guid")
            .Type(SchemaValueType.String)
            .Format(Formats.Uuid),

        [typeof(Uri).Name] = (builder) => builder
            .Comment("Uri")
            .Type(SchemaValueType.String)
            .Format(Formats.Uri),

        [typeof(DateTimeOffset).Name] = (builder) => builder
            .Comment("DateTimeOffset")
            .Type(SchemaValueType.String)
            .Format(Formats.DateTime),

        [typeof(TimeOnly).Name] = (builder) => builder
            .Comment("TimeOnly")
            .Type(SchemaValueType.String)
            .Format(Formats.Time),

        [typeof(DateOnly).Name] = (builder) => builder
            .Comment("DateOnly")
            .Type(SchemaValueType.String)
            .Format(Formats.Date),
    };

    /// <summary>
    /// Converts a <see cref="Type"/> to a JSON schema.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="isTopLevel">Indicates whether the current type is the top-level type.</param>
    /// <param name="propertyAttributeData">The attribute data from the owning property.</param>
    /// <returns>The JSON schema represented by a <see cref="JsonSchemaBuilder"/>.</returns>
    public static JsonSchemaBuilder AddType(this JsonSchemaBuilder builder, Type type, ConverterContext context, bool isTopLevel = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        Requires.NotNull(context, nameof(context));

        try
        {
            if (++context.CurrentDepth > context.MaxDepth)
            {
                throw new InvalidOperationException($"Exceeded object graph depth of {context.MaxDepth}.");
            }

            Requires.NotNull(builder, nameof(builder));
            Requires.NotNull(type, nameof(type));
            Requires.NotNull(context, nameof(context));

            // unwrap nullable value types
            if (type.TryUnwrapNullable(out Type? unwrappedType))
            {
                return builder.AddType(unwrappedType, context, isTopLevel, propertyAttributeData);
            }

            // if the type has an AmbientValueAttribute with a string value, use that as the type schema
            AmbientValueTypeHandler ambientValueTypeHandler = new();
            if (ambientValueTypeHandler.TryHandle(builder, context, type, isTopLevel, propertyAttributeData) is { IsHandled: true } handledResult)
            {
                return handledResult.Builder;
            }

            // handle enums as strings, or continue and process as the type code for the underlying numeric type
            EnumAsStringTypeHandler enumAsStringTypeHandler = new();
            if (enumAsStringTypeHandler.TryHandle(builder, context, type, isTopLevel, propertyAttributeData) is { IsHandled: true } enumHandledResult)
            {
                return enumHandledResult.Builder;
            }

            // handle remaining value types and complex objects
            TypeCodeTypeHandler typeCodeTypeHandler = new();
            if (typeCodeTypeHandler.TryHandle(builder, context, type, isTopLevel, propertyAttributeData) is { IsHandled: true } typeCodeHandledResult)
            {
                return typeCodeHandledResult.Builder;
            }

            return builder.AddObjectType(type, isTopLevel, context, propertyAttributeData);
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
                            isTopLevel: false,
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
                    isTopLevel: false,
                    propertyAttributeData: property.GetCustomAttributesData())
                .AddPropertyConstraints(property);
        }

        return builder
            .Title(property.Name.Titleize())
            .AddPropertyAnnotations(property);
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
    internal static JsonSchemaBuilder AddPropertyAnnotations(this JsonSchemaBuilder builder, PropertyInfo property)
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

    private static JsonSchemaBuilder AddObjectType(this JsonSchemaBuilder builder, Type type, bool isTopLevel, ConverterContext context, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        // handle arrays
        if (type.IsArray)
        {
            Type elementType = type.GetElementType() ?? Assumes.NotReachable<Type>();
            return builder.AddArrayType(elementType, context);
        }

        // handle other generics
        if (type.IsGenericType)
        {
            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            Type[] genericArguments = type.GetGenericArguments();

            if (genericTypeDefinition.ImplementsAnyInterface(OpenDictionaryGenericTypeNames))
            {
                // handle dictionaries
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
                    .AddType(keyType, context);

                // if the property has a regular expression attribute, use that as the key pattern
                if (propertyAttributeData is not null)
                {
                    CustomAttributeData? regexAttribute = propertyAttributeData.FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.RegularExpressionAttribute");
                    if (regexAttribute is not null && regexAttribute.ConstructorArguments.Count == 1)
                    {
                        keySchema.Pattern((string)regexAttribute.ConstructorArguments[0].Value!);
                    }
                }

                return valueType is not null
                    ? builder
                        .Comment($"[{keyType.Name} = {valueType.Name}]")
                        .Type(SchemaValueType.Object)
                        .PropertyNames(keySchema)
                        .AdditionalProperties(new JsonSchemaBuilder()
                            .AddType(valueType, context))
                    : builder
                        .Comment($"[{keyType.Name}]")
                        .Type(SchemaValueType.Object)
                        .PropertyNames(keySchema);
            }

            // handle enumerable
            if (genericTypeDefinition.ImplementsAnyInterface((typeof(IEnumerable).Namespace, typeof(IEnumerable).Name)))
            {
                Type elementType = genericArguments.Single();
                return builder.AddArrayType(elementType, context);
            }

            // add oneOf for generic types
            string genericTypeDefinitionName = genericTypeDefinition.ToDefinitionName();
            string definitionName = type.ToDefinitionName();

            if (context.Defs.TryGetValue(genericTypeDefinitionName, out JsonSchemaBuilder? genericTypeBuilder))
            {
                JsonSchema genericTypeSchema = genericTypeBuilder;
                IReadOnlyCollection<JsonSchema>? existingOneOf = genericTypeSchema.GetOneOf();
                List<JsonSchema> oneOf = existingOneOf is null ? new() : new(existingOneOf);

                Uri refPath = new($"#/$defs/{definitionName}", UriKind.RelativeOrAbsolute);

                if (!oneOf.Any(s => s.GetRef() == refPath))
                {
                    oneOf.Add(new JsonSchemaBuilder()
                        .Ref(refPath));

                    context.Defs[genericTypeDefinitionName] = genericTypeBuilder
                        .OneOf(oneOf);
                }
            }
            else
            {
                genericTypeBuilder = new JsonSchemaBuilder()
                    .AddTypeAnnotations(type)
                    .OneOf(new JsonSchemaBuilder()
                        .Ref($"#/$defs/{definitionName}"));

                context.Defs.Add(genericTypeDefinitionName, genericTypeBuilder);
            }

            isTopLevel = false;
        }

        // handle specific types with string formats
        if (StringFormatSchemas.TryGetValue(type.Name, out Func<JsonSchemaBuilder, JsonSchemaBuilder>? stringFormatSchema))
        {
            return stringFormatSchema(builder);
        }

        return builder.AddComplexType(type, context, isTopLevel);
    }

    private static JsonSchemaBuilder AddArrayType(this JsonSchemaBuilder builder, Type itemType, ConverterContext context)
    {
        JsonSchemaBuilder itemSchema = new JsonSchemaBuilder().AddType(itemType, context);

        return builder
            .Comment($"{itemType.Name}[]")
            .Type(SchemaValueType.Array)
            .Items(itemSchema)
            .AdditionalItems(false);
    }

    private static JsonSchemaBuilder AddComplexType(this JsonSchemaBuilder builder, Type type, ConverterContext context, bool isTopLevel)
    {
        if (isTopLevel)
        {
            return AddCustomObjectType(builder, type, context);
        }

        string defName = type.ToDefinitionName();
        if (context.Defs.TryAdd(defName, AddCustomObjectType(new JsonSchemaBuilder(), type, context)) &&
            context.IncludeInterfaces)
        {
            type.AddToInterfaces(defName, context.Defs);
        }

        return builder
            .Ref($"#/$defs/{defName}");

        //// -- local function --

        static JsonSchemaBuilder AddCustomObjectType(JsonSchemaBuilder builder, Type type, ConverterContext context)
        {
            builder = builder.AddTypeAnnotations(type);

            if (type.IsAbstract)
            {
                Assembly assembly = type.Assembly;

                var concreteTypes = assembly.GetTypes()
                    .Where(t => t.IsSubclassOf(type) && !t.IsAbstract)
                    .Select(t => new JsonSchemaBuilder().AddType(t, context).Build())
                    .ToList();

                if (concreteTypes.Count == 0)
                {
                    return builder;
                }

                return builder
                    .OneOf(concreteTypes);
            }

            Dictionary<string, JsonSchema> propertySchemas = type.GetObjectPropertySchemas(context, out IEnumerable<string>? requiredProperties);

            return builder
                .Type(SchemaValueType.Object)
                .Properties(propertySchemas)
                .Required(requiredProperties)
                .AdditionalProperties(false);
        }
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
