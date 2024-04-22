// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Json.Schema;

namespace SharpSchema;

/// <summary>
/// Initializes a new instance of the <see cref="ObjectSchemaGenerator"/> class.
/// </summary>
internal class ObjectSchemaGenerator(Type type, Dictionary<string, JsonSchemaBuilder> defs)
{
    /// <summary>
    /// Adds an object schema to the <see cref="JsonSchemaBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="JsonSchemaBuilder"/> instance.</param>
    /// <param name="depth">The current depth of the object graph.</param>
    /// <returns>The updated <see cref="JsonSchemaBuilder"/> instance.</returns>
    public JsonSchemaBuilder AddObject(JsonSchemaBuilder builder, int depth)
    {
        builder = builder.AddTypeAnnotations(type);

        if (type.IsAbstract)
        {
            Assembly assembly = type.Assembly;

            var concreteTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(type) && !t.IsAbstract)
                .Select(t => new JsonSchemaBuilder().AddType(t, defs, depth).Build())
                .ToList();

            if (concreteTypes.Count == 0)
            {
                return builder;
            }

            return builder
                .OneOf(concreteTypes);
        }

        Dictionary<string, JsonSchema> propertySchemas = this.GetObjectProperties(depth, out IEnumerable<string>? requiredProperties);
        builder = builder
            .Type(SchemaValueType.Object)
            .Properties(propertySchemas)
            .Required(requiredProperties)
            .AdditionalProperties(false);

        return builder;
    }

    private Dictionary<string, JsonSchema> GetObjectProperties(int depth, out IEnumerable<string> required)
    {
        List<string>? requiredProperties = null;

        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
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

            propertySchemas[normalizedName] = GetPropertySchema(property, normalizedName, defs, depth, ref requiredProperties);
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

            propertySchemas[normalizedName] = GetPropertySchema(property, normalizedName, defs, depth, ref requiredProperties);
        }

        required = requiredProperties ?? Enumerable.Empty<string>();

        return propertySchemas;

        //// -- local functions --

        static JsonSchemaBuilder GetPropertySchema(PropertyInfo property, string normalizedName, Dictionary<string, JsonSchemaBuilder> defs, int depth, ref List<string>? requiredProperties)
        {
            JsonSchemaBuilder propertySchema = new JsonSchemaBuilder()
                .AddPropertyInfo(property, defs, depth, out bool isRequired);

            if (isRequired)
            {
                requiredProperties ??= [];
                requiredProperties.Add(normalizedName);
            }

            return propertySchema;
        }
    }
}
