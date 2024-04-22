// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Json.Schema;
using Microsoft;

namespace SharpSchema;

/// <summary>
/// Provides extension methods for converting a <see cref="Type"/> to a JSON schema.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Converts a <see cref="Type"/> to a JSON schema using the specified <see cref="JsonSchemaBuilder"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <returns>The <see cref="JsonSchemaBuilder"/> representing the JSON schema.</returns>
    public static JsonSchemaBuilder ToJsonSchema(this Type type)
    {
        Requires.NotNull(type, nameof(type));

        JsonSchemaBuilder builder = new JsonSchemaBuilder()
            .Schema("http://json-schema.org/draft-07/schema#");

        // if the type has a DisplayName attribute, use it as the $id
        if (type.GetCustomAttributesData()
            .FirstOrDefault(a => a.AttributeType.FullName == "System.ComponentModel.DisplayNameAttribute") is { ConstructorArguments: { Count: 1 } arguments })
        {
            builder = builder.Id((string)arguments[0].Value!);
        }

        return type.ToJsonSchema(builder);
    }

    /// <summary>
    /// Converts a <see cref="Type"/> to a JSON schema using the specified <see cref="JsonSchemaBuilder"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <param name="builder">The <see cref="JsonSchemaBuilder"/> to use for building the JSON schema.</param>
    /// <returns>The <see cref="JsonSchemaBuilder"/> representing the JSON schema.</returns>
    public static JsonSchemaBuilder ToJsonSchema(this Type type, JsonSchemaBuilder builder)
    {
        Dictionary<string, JsonSchemaBuilder> defs = [];
        builder = builder.AddType(type, defs, isTopLevel: true);

        if (defs.Count > 0)
        {
            builder = builder
                .Defs(defs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build()));
        }

        return builder;
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
    /// Adds the type to the interfaces in the JSON schema definitions.
    /// </summary>
    /// <param name="type">The type to add.</param>
    /// <param name="defName">The definition name for JSON schema.</param>
    /// <param name="defs">The dictionary of JSON schema definitions.</param>
    internal static void AddToInterfaces(this Type type, string defName, Dictionary<string, JsonSchemaBuilder> defs)
    {
        foreach (Type iface in type.GetInterfaces())
        {
            if (iface.GetCustomAttributesData()
                .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
            {
                continue;
            }

            string ifaceDefName = iface.ToDefinitionName();
            if (defs.TryGetValue(ifaceDefName, out JsonSchemaBuilder? ifaceSchemaBuilder))
            {
                JsonSchema ifaceSchema = ifaceSchemaBuilder;

                IReadOnlyCollection<JsonSchema>? existingOneOf = ifaceSchema.GetOneOf();
                List<JsonSchema> oneOf = existingOneOf is null ? new() : new(existingOneOf);
                oneOf.Add(new JsonSchemaBuilder()
                    .Ref($"#/$defs/{defName}"));

                defs[ifaceDefName] = ifaceSchemaBuilder
                    .OneOf(oneOf);
            }
            else
            {
                ifaceSchemaBuilder = new JsonSchemaBuilder()
                    .AddTypeAnnotations(iface)
                    .OneOf(new JsonSchemaBuilder()
                        .Ref($"#/$defs/{defName}"));

                defs.Add(ifaceDefName, ifaceSchemaBuilder);
            }
        }
    }
}
