// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using Humanizer;
using Json.Schema;
using Microsoft;

namespace SharpSchema;

/// <summary>
/// Provides extension methods for converting a <see cref="Type"/> to a JSON schema.
/// </summary>
public static class TypeExtensions
{
    private const int MaxDepth = 50;

    private static readonly Dictionary<Type, JsonSchemaBuilder> StringFormatSchemas = new(capacity: 5)
    {
        [typeof(Guid)] = new JsonSchemaBuilder().Type(SchemaValueType.String).Format(Formats.Uuid).Comment("Guid"),
        [typeof(Uri)] = new JsonSchemaBuilder().Type(SchemaValueType.String).Format(Formats.Uri).Comment("Uri"),
        [typeof(DateTimeOffset)] = new JsonSchemaBuilder().Type(SchemaValueType.String).Format(Formats.DateTime).Comment("DateTimeOffset"),
        [typeof(TimeOnly)] = new JsonSchemaBuilder().Type(SchemaValueType.String).Format(Formats.Time).Comment("TimeOnly"),
        [typeof(DateOnly)] = new JsonSchemaBuilder().Type(SchemaValueType.String).Format(Formats.Date).Comment("DateOnly"),
    };

    /// <summary>
    /// Converts a <see cref="Type"/> to a JSON schema.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <returns>The JSON schema represented by a <see cref="JsonSchemaBuilder"/>.</returns>
    public static JsonSchemaBuilder ToJsonSchema(this Type type)
    {
        Dictionary<string, JsonSchemaBuilder> defs = [];
        JsonSchemaBuilder typeSchema = type.ToJsonSchemaInternal(defs, depth: 0, isTopLevel: true);
        if (defs.Count > 0)
        {
            typeSchema
                .Defs(defs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build()));
        }

        return typeSchema;
    }

    /// <summary>
    /// Converts a <see cref="TypeCode"/> to a <see cref="SchemaValueType"/>.
    /// </summary>
    /// <param name="typeCode">The <see cref="TypeCode"/> to convert.</param>
    /// <returns>The corresponding <see cref="SchemaValueType"/>.</returns>
    public static SchemaValueType ToSchemaValueType(this TypeCode typeCode) => typeCode switch
    {
        TypeCode.Boolean => SchemaValueType.Boolean,
        TypeCode.Byte => SchemaValueType.Integer,
        TypeCode.Char => SchemaValueType.Integer,
        TypeCode.DateTime => SchemaValueType.String,
        TypeCode.Decimal => SchemaValueType.Number,
        TypeCode.Double => SchemaValueType.Number,
        TypeCode.Int16 => SchemaValueType.Integer,
        TypeCode.Int32 => SchemaValueType.Integer,
        TypeCode.Int64 => SchemaValueType.Integer,
        TypeCode.SByte => SchemaValueType.Integer,
        TypeCode.Single => SchemaValueType.Number,
        TypeCode.String => SchemaValueType.String,
        TypeCode.UInt16 => SchemaValueType.Integer,
        TypeCode.UInt32 => SchemaValueType.Integer,
        TypeCode.UInt64 => SchemaValueType.Integer,
        TypeCode.Object => SchemaValueType.Object,
        _ => Assumes.NotReachable<SchemaValueType>(),
    };

    /// <summary>
    /// Converts a <see cref="Type"/> to a definition name for JSON schema.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <returns>The definition name for JSON schema.</returns>
    public static string ToDefinitionName(this Type type)
    {
        Requires.NotNull(type, nameof(type));

        return (type.FullName ?? type.Name)
            .Replace('+', '_')
            .Replace('\'', '_');
    }

    /// <summary>
    /// Converts a <see cref="Type"/> to a JSON schema.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <param name="defs">The dictionary of definitions used in the JSON schema.</param>
    /// <param name="depth">The current depth of the object graph.</param>
    /// <param name="isTopLevel">Indicates whether the current type is the top-level type.</param>
    /// <param name="propertyAttributeData">The attribute data from the owning property.</param>
    /// <returns>The JSON schema represented by a <see cref="JsonSchemaBuilder"/>.</returns>
    internal static JsonSchemaBuilder ToJsonSchemaInternal(this Type type, Dictionary<string, JsonSchemaBuilder> defs, int depth, bool isTopLevel = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        depth += 1;
        Requires.Range(depth < MaxDepth, nameof(depth), $"Exceeded object graph depth of {MaxDepth}.");
        Requires.NotNull(type, nameof(type));
        Requires.NotNull(defs, nameof(defs));

        // handle nullable types
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            Type underlyingType = Nullable.GetUnderlyingType(type) ?? Assumes.NotReachable<Type>();
            return underlyingType.ToJsonSchemaInternal(defs, depth);
        }

        // handle arrays
        if (type.IsArray)
        {
            Type elementType = type.GetElementType() ?? Assumes.NotReachable<Type>();
            return CreateArraySchema(elementType, defs, depth);
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
                JsonSchemaBuilder keySchema = keyType.ToJsonSchemaInternal(defs, depth);

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
                JsonSchemaBuilder valueSchema = valueType.ToJsonSchemaInternal(defs, depth);

                return new JsonSchemaBuilder()
                    .Type(SchemaValueType.Object)
                    .AdditionalProperties(valueSchema)
                    .PropertyNames(keySchema);
            }

            // handle lists
            Type elementType = genericArguments.Single();
            return CreateArraySchema(elementType, defs, depth);
        }

        // handle enums
        if (type.IsEnum)
        {
            string[] enumNames = Enum.GetNames(type);
            string[] kebabCaseEnumNames = new string[enumNames.Length];
            for (int i = 0; i < enumNames.Length; i++)
            {
                kebabCaseEnumNames[i] = enumNames[i].Kebaberize();
            }

            return new JsonSchemaBuilder()
                .Comment(type.Name)
                .Type(SchemaValueType.String)
                .Enum(kebabCaseEnumNames);
        }

        // handle specific types with string formats
        if (StringFormatSchemas.TryGetValue(type, out JsonSchemaBuilder? stringFormatSchema))
        {
            return stringFormatSchema;
        }

        // handle remaining value types and complex objects
        switch (Type.GetTypeCode(type))
        {
            case TypeCode.Boolean:
                return new JsonSchemaBuilder()
                    .Comment("Boolean")
                    .Type(TypeCode.Boolean.ToSchemaValueType());

            case TypeCode.Byte:
                return new JsonSchemaBuilder()
                    .Comment("Byte")
                    .Type(TypeCode.Byte.ToSchemaValueType())
                    .Minimum(byte.MinValue)
                    .Maximum(byte.MaxValue);

            case TypeCode.Char:
                return new JsonSchemaBuilder()
                    .Comment("Char")
                    .Type(TypeCode.Char.ToSchemaValueType())
                    .Minimum(char.MinValue)
                    .Maximum(char.MaxValue);

            case TypeCode.DateTime:
                return new JsonSchemaBuilder()
                    .Comment("DateTime")
                    .Type(TypeCode.DateTime.ToSchemaValueType())
                    .Format(Formats.DateTime);

            case TypeCode.Decimal:
                return new JsonSchemaBuilder()
                    .Comment("Decimal")
                    .Type(TypeCode.Decimal.ToSchemaValueType())
                    .Minimum(decimal.MinValue)
                    .Maximum(decimal.MaxValue);

            case TypeCode.Double:
                return new JsonSchemaBuilder()
                    .Comment("Double")
                    .Type(TypeCode.Double.ToSchemaValueType())
                    .Minimum(decimal.CreateSaturating(double.MinValue))
                    .Maximum(decimal.CreateSaturating(double.MaxValue));

            case TypeCode.Int16:
                return new JsonSchemaBuilder()
                    .Comment("Int16")
                    .Type(TypeCode.Int16.ToSchemaValueType())
                    .Minimum(short.MinValue)
                    .Maximum(short.MaxValue);

            case TypeCode.Int32:
                return new JsonSchemaBuilder()
                    .Comment("Int32")
                    .Type(TypeCode.Int32.ToSchemaValueType())
                    .Minimum(int.MinValue)
                    .Maximum(int.MaxValue);

            case TypeCode.Int64:
                return new JsonSchemaBuilder()
                    .Comment("Int64")
                    .Type(TypeCode.Int64.ToSchemaValueType())
                    .Minimum(long.MinValue)
                    .Maximum(long.MaxValue);

            case TypeCode.SByte:
                return new JsonSchemaBuilder()
                    .Comment("SByte")
                    .Type(TypeCode.SByte.ToSchemaValueType())
                    .Minimum(sbyte.MinValue)
                    .Maximum(sbyte.MaxValue);

            case TypeCode.Single:
                return new JsonSchemaBuilder()
                    .Comment("Single")
                    .Type(TypeCode.Single.ToSchemaValueType())
                    .Minimum(decimal.CreateSaturating(float.MinValue))
                    .Maximum(decimal.CreateSaturating(float.MaxValue));

            case TypeCode.String:
                return new JsonSchemaBuilder()
                    .Comment("String")
                    .Type(TypeCode.String.ToSchemaValueType());

            case TypeCode.UInt16:
                return new JsonSchemaBuilder()
                    .Comment("UInt16")
                    .Type(TypeCode.UInt16.ToSchemaValueType())
                    .Minimum(ushort.MinValue)
                    .Maximum(ushort.MaxValue);

            case TypeCode.UInt32:
                return new JsonSchemaBuilder()
                    .Comment("UInt32")
                    .Type(TypeCode.UInt32.ToSchemaValueType())
                    .Minimum(uint.MinValue)
                    .Maximum(uint.MaxValue);

            case TypeCode.UInt64:
                return new JsonSchemaBuilder()
                    .Comment("UInt64")
                    .Type(TypeCode.UInt64.ToSchemaValueType())
                    .Minimum(ulong.MinValue)
                    .Maximum(ulong.MaxValue);

            case TypeCode.Object:
                return new ObjectSchemaGenerator(type, defs)
                    .GetSchema(isTopLevel, depth);

            default:
                return Assumes.NotReachable<JsonSchemaBuilder>();
        }
    }

    private static JsonSchemaBuilder CreateArraySchema(Type itemType, Dictionary<string, JsonSchemaBuilder> defs, int depth)
    {
        JsonSchemaBuilder itemSchema = itemType.ToJsonSchemaInternal(defs, depth);

        return new JsonSchemaBuilder()
            .Type(SchemaValueType.Array)
            .Items(itemSchema)
            .AdditionalItems(false);
    }
}
