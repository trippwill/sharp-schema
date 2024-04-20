// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Humanizer;
using Json.Schema;

namespace SharpSchema;

/// <summary>
/// Initializes a new instance of the <see cref="ObjectSchemaGenerator"/> class.
/// </summary>
internal class ObjectSchemaGenerator(Type type, Dictionary<string, JsonSchemaBuilder> defs)
{
    /// <summary>
    /// Gets the JSON schema for the specified object type.
    /// </summary>
    /// <param name="isTopLevel">Indicates whether the object is a top-level schema.</param>
    /// <param name="depth">The depth of the schema.</param>
    /// <returns>The JSON schema for the object.</returns>
    public JsonSchemaBuilder GetSchema(bool isTopLevel, int depth)
    {
        JsonSchemaBuilder typeSchema = this.CreateObjectSchema(depth);

        if (isTopLevel)
        {
            return typeSchema;
        }

        string defName = type.ToDefinitionName();
        if (defs.TryAdd(defName, typeSchema))
        {
            this.AddToInterfaces(defName);
        }

        return new JsonSchemaBuilder()
            .Ref($"#/$defs/{defName}");
    }

    private JsonSchemaBuilder CreateObjectSchema(int depth)
    {
        JsonSchemaBuilder typeSchema = new JsonSchemaBuilder()
            .Comment(type.Name)
            .Type(SchemaValueType.Object);

        if (type.IsAbstract)
        {
            Assembly assembly = type.Assembly;

            var concreteTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(type) && !t.IsAbstract)
                .Select(t => t.ToJsonSchemaInternal(defs, depth).Build())
                .ToList();

            if (concreteTypes.Count == 0)
            {
                return typeSchema;
            }

            return typeSchema
                .OneOf(concreteTypes);
        }

        Dictionary<string, JsonSchema> propertySchemas = this.GetObjectProperties(depth, out IEnumerable<string>? requiredProperties);
        typeSchema = typeSchema
            .Properties(propertySchemas)
            .Required(requiredProperties)
            .AdditionalProperties(false);

        return typeSchema;
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

            JsonSchemaBuilder propertySchema = this.CreatePropertySchema(normalizedName, property, depth, ref requiredProperties);
            propertySchemas[normalizedName] = propertySchema;
        }

        // add non-public properties with JsonIncludeAttribute
        properties = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
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

            JsonSchemaBuilder propertySchema = this.CreatePropertySchema(normalizedName, property, depth, ref requiredProperties);
            propertySchemas[normalizedName] = propertySchema;
        }

        required = requiredProperties ?? Enumerable.Empty<string>();
        return propertySchemas;
    }

    private JsonSchemaBuilder CreatePropertySchema(string normalizedName, PropertyInfo property, int depth, ref List<string>? requiredProperties)
    {
        JsonSchemaBuilder propertySchema = property.PropertyType
            .ToJsonSchemaInternal(
                defs,
                depth,
                isTopLevel: false,
                propertyAttributeData: property.GetCustomAttributesData());

        property.SetRange(propertySchema);

        if (property.IsRequired(out bool isNullable))
        {
            requiredProperties ??= [];
            requiredProperties.Add(normalizedName);

            if (isNullable)
            {
                propertySchema = new JsonSchemaBuilder()
                    .OneOf(propertySchema, new JsonSchemaBuilder()
                        .Type(SchemaValueType.Null));
            }
        }

        // if the property has a DescriptionAttribute, add it to the schema
        CustomAttributeData? displayData = property.GetCustomAttributesData()
            .FirstOrDefault(a => a.AttributeType.FullName == "System.ComponentModel.DataAnnotations.DisplayAttribute");

        propertySchema = propertySchema
            .Title(property.Name.Titleize());

        if (displayData is not null)
        {
            CustomAttributeNamedArgument nameArgument = displayData.NamedArguments.FirstOrDefault(descriptionData => descriptionData.MemberName == "Name");
            if (nameArgument != default)
            {
                string name = (string)nameArgument.TypedValue.Value!;
                propertySchema = propertySchema.Title(name);
            }

            CustomAttributeNamedArgument descriptionArgument = displayData.NamedArguments.FirstOrDefault(descriptionData => descriptionData.MemberName == "Description");
            if (descriptionArgument != default)
            {
                string description = (string)descriptionArgument.TypedValue.Value!;
                propertySchema = propertySchema.Description(description);
            }
        }

        return propertySchema;
    }

    private void AddToInterfaces(string defName)
    {
        foreach (Type iface in type.GetInterfaces())
        {
            if (iface.GetCustomAttributesData()
                .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute"))
            {
                continue;
            }

            if (iface.GetCustomAttributesData()
                .Any(a => a.AttributeType.FullName == "System.Runtime.InteropServices.ComVisibleAttribute"))
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
                defs.Add(ifaceDefName, new JsonSchemaBuilder()
                    .OneOf(new JsonSchemaBuilder()
                        .Ref($"#/$defs/{defName}")));
            }
        }
    }
}
