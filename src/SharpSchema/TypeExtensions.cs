// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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
    /// <param name="context">The converter context.</param>
    /// <returns>The <see cref="JsonSchemaBuilder"/> representing the JSON schema.</returns>
    public static JsonSchemaBuilder ToJsonSchema(this Type type, ConverterContext? context = null)
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

        return type.ToJsonSchema(builder, context);
    }

    /// <summary>
    /// Converts a <see cref="Type"/> to a JSON schema using the specified <see cref="JsonSchemaBuilder"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <param name="builder">The <see cref="JsonSchemaBuilder"/> to use for building the JSON schema.</param>
    /// <param name="context">The converter context.</param>
    /// <returns>The <see cref="JsonSchemaBuilder"/> representing the JSON schema.</returns>
    public static JsonSchemaBuilder ToJsonSchema(this Type type, JsonSchemaBuilder builder, ConverterContext? context = null)
    {
        context ??= new();
        builder = builder.AddType(type, context, isTopLevel: true);

        if (context.Defs.Count > 0)
        {
            builder = builder
                .Defs(context.Defs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build()));
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

        string? genericArgs = type.IsGenericType
            ? $"[{string.Join('_', type.GetGenericArguments().Select(ToDefinitionName))}]"
            : string.Empty;

        return $"{type.Namespace}.{(type.DeclaringType is null ? string.Empty : (type.DeclaringType.Name + '_'))}{type.Name}{genericArgs}"
            .Replace('+', '_')
            .Replace('`', '_');
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
            // ignore system interfaces
            if (iface.Namespace is null || iface.Namespace == "System" || iface.Namespace.StartsWith("System."))
            {
                continue;
            }

            // ignore compiler-generated interfaces
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

    /// <summary>
    /// Determines whether the specified <see cref="Type"/> implements any of the specified interface names.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="interfaceNames">The names of the interfaces to check for.</param>
    /// <returns><c><see langword="true"/></c> if the <see cref="Type"/> implements any of the specified interface names; otherwise, <see langword="false"/>.</returns>
    internal static bool ImplementsAnyInterface(this Type type, params (string? Namespace, string Name)[] interfaceNames)
    {
        if (interfaceNames.Contains((type.Namespace, type.Name)))
        {
            return true;
        }

        Type? test = type;
        while (test is not null)
        {
            foreach (Type iface in test.GetInterfaces())
            {
                if (interfaceNames.Contains((iface.Namespace, iface.Name)))
                {
                    return true;
                }
            }

            test = test.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Tries to unwrap a nullable type and get its underlying type.
    /// </summary>
    /// <param name="type">The type to unwrap.</param>
    /// <param name="underlyingType">When this method returns, contains the underlying type of the nullable type, if successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the type is a nullable type and the underlying type is successfully obtained; otherwise, <see langword="false"/>.</returns>
    internal static bool TryUnwrapNullable(this Type type, [NotNullWhen(true)] out Type? underlyingType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition().Name == typeof(Nullable<>).Name)
        {
            underlyingType = type.GetGenericArguments()[0];
            return true;
        }

        underlyingType = null;
        return false;
    }

    /// <summary>
    /// Tries to get the <see cref="CustomAttributeData"/> for the specified <see cref="Type"/> and attribute type.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="attributeType">The attribute type.</param>
    /// <param name="attributeData">When this method returns, contains the <see cref="CustomAttributeData"/> for the specified attribute type, if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the <see cref="CustomAttributeData"/> for the specified attribute type is found; otherwise, <see langword="false"/>.</returns>
    internal static bool TryGetCustomAttributeData(this Type type, Type attributeType, [NotNullWhen(true)] out CustomAttributeData? attributeData)
    {
        return type.TryGetCustomAttributeData(
            attributeType.FullName ?? throw new InvalidOperationException("Attribute type has no full name."),
            out attributeData);
    }

    /// <summary>
    /// Tries to get the <see cref="CustomAttributeData"/> for the specified <see cref="Type"/> and attribute type.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="attributeFullName">The full name of the attribute type.</param>
    /// <param name="attributeData">When this method returns, contains the <see cref="CustomAttributeData"/> for the specified attribute type, if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the <see cref="CustomAttributeData"/> for the specified attribute type is found; otherwise, <see langword="false"/>.</returns>
    internal static bool TryGetCustomAttributeData(this Type type, string attributeFullName, [NotNullWhen(true)] out CustomAttributeData? attributeData)
    {
        attributeData = type.GetCustomAttributesData()
            .FirstOrDefault(cad => cad.AttributeType.FullName == attributeFullName);

        return attributeData is not null;
    }

    /// <summary>
    /// Tries to get the ambient value for the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="ambientValue">The ambient value if present.</param>
    /// <returns><see langword="true"/> if the ambient value is found; otherwise, <see langword="false"/>.</returns>
    internal static bool TryGetAmbientValue(this Type type, [NotNullWhen(true)] out string? ambientValue)
    {
        if (type.TryGetCustomAttributeData("System.ComponentModel.AmbientValueAttribute", out CustomAttributeData? ambientValueAttribute)
            && ambientValueAttribute.ConstructorArguments.Count == 1
            && ambientValueAttribute.ConstructorArguments[0].Value is string value)
        {
            ambientValue = value;
            return true;
        }

        ambientValue = null;
        return false;
    }

    /// <summary>
    /// Gets the JSON schema property schemas for an object type.
    /// </summary>
    /// <param name="type">The object type.</param>
    /// <param name="context">The context.</param>
    /// <param name="required">The required property names.</param>
    /// <returns>The dictionary of property schemas.</returns>
    internal static Dictionary<string, JsonSchema> GetObjectPropertySchemas(this Type type, ConverterContext context, out IEnumerable<string> required)
    {
        List<string>? requiredProperties = null;

        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Dictionary<string, JsonSchema> propertySchemas = new(properties.Length);
        foreach (PropertyInfo property in properties)
        {
            if (property.ShouldSkipProperty())
            {
                continue;
            }

            // skip properties with no public getter
            MethodInfo? getMethod = property.GetGetMethod();
            if (getMethod is null || getMethod.IsPublic == false)
            {
                continue;
            }

            string normalizedName = property.GetPropertyName();

            propertySchemas[normalizedName] = GetPropertySchema(property, normalizedName, context, ref requiredProperties);
        }

        // add non-public properties with JsonIncludeAttribute
        properties = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        propertySchemas.EnsureCapacity(propertySchemas.Count + properties.Length);

        foreach (PropertyInfo property in properties)
        {
            // skip properties that have already been added
            string normalizedName = property.GetPropertyName();
            if (propertySchemas.ContainsKey(normalizedName))
            {
                continue;
            }

            if (property.ShouldSkipProperty())
            {
                continue;
            }

            // skip properties without JsonIncludeAttribute
            if (property.GetCustomAttributesData()
                .All(a => a.AttributeType.FullName != "System.Text.Json.Serialization.JsonIncludeAttribute"))
            {
                continue;
            }

            propertySchemas[normalizedName] = GetPropertySchema(property, normalizedName, context, ref requiredProperties);
        }

        required = requiredProperties ?? Enumerable.Empty<string>();

        return propertySchemas;

        //// -- local functions --

        static JsonSchemaBuilder GetPropertySchema(PropertyInfo property, string normalizedName, ConverterContext context, ref List<string>? requiredProperties)
        {
            JsonSchemaBuilder propertySchema = new JsonSchemaBuilder()
                .AddPropertyInfo(property, context, out bool isRequired);

            if (isRequired)
            {
                requiredProperties ??= [];
                requiredProperties.Add(normalizedName);
            }

            return propertySchema;
        }
    }
}
