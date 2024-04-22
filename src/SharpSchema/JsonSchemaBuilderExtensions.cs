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
    private const int MaxDepth = 50;

    private static readonly Dictionary<Type, Func<JsonSchemaBuilder, JsonSchemaBuilder>> StringFormatSchemas = new(capacity: 5)
    {
        [typeof(Guid)] = (builder) => builder
            .Comment("Guid")
            .Type(SchemaValueType.String)
            .Format(Formats.Uuid),

        [typeof(Uri)] = (builder) => builder
            .Comment("Uri")
            .Type(SchemaValueType.String)
            .Format(Formats.Uri),

        [typeof(DateTimeOffset)] = (builder) => builder
            .Comment("DateTimeOffset")
            .Type(SchemaValueType.String)
            .Format(Formats.DateTime),

        [typeof(TimeOnly)] = (builder) => builder
            .Comment("TimeOnly")
            .Type(SchemaValueType.String)
            .Format(Formats.Time),

        [typeof(DateOnly)] = (builder) => builder
            .Comment("DateOnly")
            .Type(SchemaValueType.String)
            .Format(Formats.Date),
    };

    /// <summary>
    /// Converts a <see cref="Type"/> to a JSON schema.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <param name="defs">The dictionary of definitions used in the JSON schema.</param>
    /// <param name="depth">The current depth of the object graph.</param>
    /// <param name="isTopLevel">Indicates whether the current type is the top-level type.</param>
    /// <param name="propertyAttributeData">The attribute data from the owning property.</param>
    /// <returns>The JSON schema represented by a <see cref="JsonSchemaBuilder"/>.</returns>
    public static JsonSchemaBuilder AddType(this JsonSchemaBuilder builder, Type type, Dictionary<string, JsonSchemaBuilder> defs, int depth = 0, bool isTopLevel = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        depth += 1;

        Requires.Range(depth < MaxDepth, nameof(depth), $"Exceeded object graph depth of {MaxDepth}.");
        Requires.NotNull(builder, nameof(builder));
        Requires.NotNull(type, nameof(type));
        Requires.NotNull(defs, nameof(defs));

        // unwrap nullable value types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? Assumes.NotReachable<Type>();
            return builder.AddType(underlyingType, defs, depth);
        }

        // handle enums as strings as the type code reported is for the underlying numeric type
        if (type.IsEnum)
        {
            string[] enumNames = Enum.GetNames(type);
            string[] kebabCaseEnumNames = new string[enumNames.Length];
            for (int i = 0; i < enumNames.Length; i++)
            {
                kebabCaseEnumNames[i] = enumNames[i].Kebaberize();
            }

            return builder
                .Comment(type.Name)
                .Type(SchemaValueType.String)
                .Enum(kebabCaseEnumNames);
        }

        // handle remaining value types and complex objects
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => builder
                .Comment("bool")
                .Type(TypeCode.Boolean.ToSchemaValueType()),
            TypeCode.Byte => builder
                .Comment("byte")
                .Type(TypeCode.Byte.ToSchemaValueType())
                .Minimum(byte.MinValue)
                .Maximum(byte.MaxValue),
            TypeCode.Char => builder
                .Comment("char")
                .Type(TypeCode.Char.ToSchemaValueType())
                .Minimum(char.MinValue)
                .Maximum(char.MaxValue),
            TypeCode.DateTime => builder
                .Comment("DateTime")
                .Type(TypeCode.DateTime.ToSchemaValueType())
                .Format(Formats.DateTime),
            TypeCode.Decimal => builder
                .Comment("decimal")
                .Type(TypeCode.Decimal.ToSchemaValueType())
                .Minimum(decimal.MinValue)
                .Maximum(decimal.MaxValue),
            TypeCode.Double => builder
                .Comment("double")
                .Type(TypeCode.Double.ToSchemaValueType())
                .Minimum(decimal.CreateSaturating(double.MinValue))
                .Maximum(decimal.CreateSaturating(double.MaxValue)),
            TypeCode.Int16 => builder
                .Comment("short")
                .Type(TypeCode.Int16.ToSchemaValueType())
                .Minimum(short.MinValue)
                .Maximum(short.MaxValue),
            TypeCode.Int32 => builder
                .Comment("int")
                .Type(TypeCode.Int32.ToSchemaValueType())
                .Minimum(int.MinValue)
                .Maximum(int.MaxValue),
            TypeCode.Int64 => builder
                .Comment("long")
                .Type(TypeCode.Int64.ToSchemaValueType())
                .Minimum(long.MinValue)
                .Maximum(long.MaxValue),
            TypeCode.SByte => builder
                .Comment("sbyte")
                .Type(TypeCode.SByte.ToSchemaValueType())
                .Minimum(sbyte.MinValue)
                .Maximum(sbyte.MaxValue),
            TypeCode.Single => builder
                .Comment("float")
                .Type(TypeCode.Single.ToSchemaValueType())
                .Minimum(decimal.CreateSaturating(float.MinValue))
                .Maximum(decimal.CreateSaturating(float.MaxValue)),
            TypeCode.String => builder
                .Comment("string")
                .Type(TypeCode.String.ToSchemaValueType()),
            TypeCode.UInt16 => builder
                .Comment("ushort")
                .Type(TypeCode.UInt16.ToSchemaValueType())
                .Minimum(ushort.MinValue)
                .Maximum(ushort.MaxValue),
            TypeCode.UInt32 => builder
                .Comment("uint")
                .Type(TypeCode.UInt32.ToSchemaValueType())
                .Minimum(uint.MinValue)
                .Maximum(uint.MaxValue),
            TypeCode.UInt64 => builder
                .Comment("ulong")
                .Type(TypeCode.UInt64.ToSchemaValueType())
                .Minimum(ulong.MinValue)
                .Maximum(ulong.MaxValue),
            TypeCode.Object => builder
                .AddObjectType(type, isTopLevel, defs, depth, propertyAttributeData),
            _ => Assumes.NotReachable<JsonSchemaBuilder>(),
        };
    }

    /// <summary>
    /// Adds property information to the JSON schema builder.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="property">The <see cref="PropertyInfo"/> representing the property.</param>
    /// <param name="defs">The dictionary of definitions used in the JSON schema.</param>
    /// <param name="depth">The current depth of the object graph.</param>
    /// <param name="isRequired">Indicates whether the property is required.</param>
    /// <returns>The updated JSON schema builder.</returns>
    public static JsonSchemaBuilder AddPropertyInfo(this JsonSchemaBuilder builder, PropertyInfo property, Dictionary<string, JsonSchemaBuilder> defs, int depth, out bool isRequired)
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
                            defs,
                            depth,
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
                    defs,
                    depth,
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
        return builder.AddAnnotations(customAttributeData, type.Name);
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
        return builder.AddAnnotations(customAttributeData);
    }

    private static JsonSchemaBuilder AddAnnotations(this JsonSchemaBuilder builder, IList<CustomAttributeData> customAttributeData, string? commentFallback = null)
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

    private static JsonSchemaBuilder AddObjectType(this JsonSchemaBuilder builder, Type type, bool isTopLevel, Dictionary<string, JsonSchemaBuilder> defs, int depth, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        // handle arrays
        if (type.IsArray)
        {
            Type elementType = type.GetElementType() ?? Assumes.NotReachable<Type>();
            return builder.AddArrayType(elementType, defs, depth);
        }

        // handle enumerable
        if (typeof(IEnumerable).IsAssignableFrom(type) && type.IsGenericType)
        {
            // handle dictionaries
            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            Type[] genericArguments = type.GetGenericArguments();

            if (genericTypeDefinition == typeof(IDictionary<,>) ||
                genericTypeDefinition == typeof(IReadOnlyDictionary<,>) ||
                genericTypeDefinition == typeof(IImmutableDictionary<,>) ||
                genericTypeDefinition == typeof(Dictionary<,>) ||
                genericTypeDefinition == typeof(ImmutableDictionary<,>))
            {
                Requires.Range(genericArguments.Length == 2, nameof(type), "Only dictionaries with two generic arguments are supported.");

                Type keyType = genericArguments[0];
                Requires.Argument(keyType == typeof(string), nameof(type), "Only dictionaries with string keys are supported.");

                JsonSchemaBuilder keySchema = new JsonSchemaBuilder()
                    .AddType(keyType, defs, depth);

                // if the property has a regular expression attribute, use that as the key pattern
                if (propertyAttributeData is not null)
                {
                    CustomAttributeData? regexAttribute = propertyAttributeData.FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.RegularExpressionAttribute");
                    if (regexAttribute is not null && regexAttribute.ConstructorArguments.Count == 1)
                    {
                        keySchema.Pattern((string)regexAttribute.ConstructorArguments[0].Value!);
                    }
                }

                Type valueType = genericArguments[1];
                JsonSchemaBuilder valueSchema = new JsonSchemaBuilder()
                    .AddType(valueType, defs, depth);

                return builder
                    .Comment($"[{keyType.Name} = {valueType.Name}]")
                    .Type(SchemaValueType.Object)
                    .PropertyNames(keySchema)
                    .AdditionalProperties(valueSchema);
            }

            // handle lists
            Type elementType = genericArguments.Single();
            return builder.AddArrayType(elementType, defs, depth);
        }

        // handle specific types with string formats
        if (StringFormatSchemas.TryGetValue(type, out Func<JsonSchemaBuilder, JsonSchemaBuilder>? stringFormatSchema))
        {
            return stringFormatSchema(builder);
        }

        return builder.AddComplexType(type, isTopLevel, defs, depth);
    }

    private static JsonSchemaBuilder AddArrayType(this JsonSchemaBuilder builder, Type itemType, Dictionary<string, JsonSchemaBuilder> defs, int depth)
    {
        JsonSchemaBuilder itemSchema = new JsonSchemaBuilder().AddType(itemType, defs, depth);

        return builder
            .Comment($"{itemType.Name}[]")
            .Type(SchemaValueType.Array)
            .Items(itemSchema)
            .AdditionalItems(false);
    }

    private static JsonSchemaBuilder AddComplexType(this JsonSchemaBuilder builder, Type type, bool isTopLevel, Dictionary<string, JsonSchemaBuilder> defs, int depth)
    {
        if (isTopLevel)
        {
            return new ObjectSchemaGenerator(type, defs)
                .AddObject(builder, depth);
        }

        string defName = type.ToDefinitionName();
        if (defs.TryAdd(defName, new ObjectSchemaGenerator(type, defs).AddObject(new JsonSchemaBuilder(), depth)))
        {
            type.AddToInterfaces(defName, defs);
        }

        return builder
            .Ref($"#/$defs/{defName}");
    }

    private static JsonSchemaBuilder AddPropertyConstraints(this JsonSchemaBuilder builder, PropertyInfo property)
    {
        Type type = property.PropertyType;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = Nullable.GetUnderlyingType(type) ?? Assumes.NotReachable<Type>();
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
        }

        // if the property is a string
        if (type == typeof(string))
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
        }

        return builder;
    }
}
