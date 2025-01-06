// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;
using Humanizer;
using Json.Schema;
using libanvl;
using SharpMeta;
using SharpSchema.Annotations;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles types that do not have a specific type handler.
/// </summary>
internal class FallbackTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(
        JsonSchemaBuilder builder,
        ConverterContext context,
        Type type,
        bool isRootType,
        Opt<PropertyInfo> propertyInfo)
    {
        try
        {
            builder = AddObjectType(builder, context, type, isRootType);
            return Result.Handled(builder);
        }
        catch (Exception ex)
        {
            return Result.Fault(builder, ex.Message);
        }
    }

    private static JsonSchemaBuilder AddObjectType(
        JsonSchemaBuilder builder,
        ConverterContext context,
        Type type,
        bool isTopLevel)
    {
        // handle other generics
        if (type.IsGenericType)
        {
            Type genericTypeDefinition = type.GetGenericTypeDefinition();

            // add oneOf for generic types
            string genericTypeDefinitionName = genericTypeDefinition.ToDefinitionName(context);
            string definitionName = type.ToDefinitionName(context);
            Uri refPath = definitionName.ToJsonDefUri();

            if (context.Defs.TryGetValue(genericTypeDefinitionName, out JsonSchemaBuilder? genericTypeBuilder))
            {
                JsonSchema genericTypeSchema = genericTypeBuilder;
                IReadOnlyCollection<JsonSchema>? existingOneOf = genericTypeSchema.GetOneOf();
                List<JsonSchema> oneOf = existingOneOf is null ? new() : new(existingOneOf);

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
                    .AddTypeAnnotations(context, type)
                    .OneOf(new JsonSchemaBuilder()
                        .Ref(refPath));

                context.Defs.Add(genericTypeDefinitionName, genericTypeBuilder);
            }

            isTopLevel = false;
        }

        return AddComplexType(builder, type, context, isTopLevel);
    }

    private static JsonSchemaBuilder AddComplexType(JsonSchemaBuilder builder, Type type, ConverterContext context, bool isRootType)
    {
        if (isRootType)
        {
            return AddCustomObjectType(builder, type, context);
        }

        string defName = type.ToDefinitionName(context);
        if (context.Defs.TryAdd(defName, AddCustomObjectType(new JsonSchemaBuilder(), type, context)) &&
            context.IncludeInterfaces)
        {
            AddToInterfaces(type, context, defName, context.Defs);
        }

        return builder.Ref(defName.ToJsonDefUri());

        //// -- local function --

        static JsonSchemaBuilder AddCustomObjectType(JsonSchemaBuilder builder, Type type, ConverterContext context)
        {
            builder = builder.AddTypeAnnotations(context, type);

            if (type.IsAbstract)
            {
                var concreteTypeSchemas = type
                    .GetSubTypes()
                    .Select(t => new JsonSchemaBuilder().AddType(context: context, type: t).Build())
                    .ToList();

                if (concreteTypeSchemas.Count == 0)
                {
                    return builder;
                }

                return builder
                    .OneOf(concreteTypeSchemas);
            }

            Dictionary<string, JsonSchema> propertySchemas = GetObjectPropertySchemas(type, context, out IEnumerable<string>? requiredProperties);

            builder = builder
                .Type(SchemaValueType.Object)
                .Properties(propertySchemas);

            if (requiredProperties is not null)
            {
                builder = builder.Required(requiredProperties);
            }

            // if the property has a properties range attribute, set the minimum and maximum properties
            Opt<CustomAttributeData> typeCad = type.GetCustomAttributeData<SchemaPropertiesRangeAttribute>();
            if (typeCad.IsSome)
            {
                Opt<uint> min = typeCad.Select(cad => cad.GetNamedArgument<uint?>(nameof(SchemaPropertiesRangeAttribute.Min)));
                if (min.IsSome)
                {
                    builder = builder.MinProperties(min.Unwrap());
                }

                Opt<uint> max = typeCad.Select(cad => cad.GetNamedArgument<uint?>(nameof(SchemaPropertiesRangeAttribute.Max)));
                if (max.IsSome)
                {
                    builder = builder.MaxProperties(max.Unwrap());
                }
            }

            return builder.AdditionalProperties(false);
        }
    }

    private static Dictionary<string, JsonSchema> GetObjectPropertySchemas(Type type, ConverterContext context, out IEnumerable<string>? required)
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
            if (!property.TryGetCustomAttributeData(typeof(JsonIncludeAttribute), out _))
            {
                continue;
            }

            propertySchemas[normalizedName] = GetPropertySchema(property, normalizedName, context, ref requiredProperties);
        }

        required = requiredProperties;

        return propertySchemas;

        //// -- local functions --

        static JsonSchemaBuilder GetPropertySchema(PropertyInfo property, string normalizedName, ConverterContext context, ref List<string>? requiredProperties)
        {
            if (property.TryGetCustomAttributeData<SchemaOverrideAttribute>(out CustomAttributeData? cad))
            {
                string? overrideValue = cad.GetConstructorArgument<string>(0)
                    ?? throw new Exception("Override value is null.");

                JsonSchemaBuilder builder = new();
                var overrideSchema = JsonSchema.FromText(overrideValue);
                foreach (IJsonSchemaKeyword keyword in overrideSchema.Keywords ?? Enumerable.Empty<IJsonSchemaKeyword>())
                {
                    builder.Add(keyword);
                }

                return builder;
            }

            JsonSchemaBuilder propertySchema = new JsonSchemaBuilder()
                .AddPropertyInfo(context, property, out bool isRequired);

            if (propertySchema.Get<TitleKeyword>() is null)
            {
                propertySchema = propertySchema.Title(property.Name.Titleize());
            }

            if (isRequired)
            {
                requiredProperties ??= [];
                requiredProperties.Add(normalizedName);
            }

            return propertySchema;
        }
    }

    private static void AddToInterfaces(
        Type type,
        ConverterContext context,
        string defName,
        SortedDictionary<string, JsonSchemaBuilder> defs)
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

            string ifaceDefName = iface.ToDefinitionName(context);
            if (defs.TryGetValue(ifaceDefName, out JsonSchemaBuilder? ifaceSchemaBuilder))
            {
                JsonSchema ifaceSchema = ifaceSchemaBuilder;

                IReadOnlyCollection<JsonSchema>? existingOneOf = ifaceSchema.GetOneOf();
                List<JsonSchema> oneOf = existingOneOf is null ? new() : new(existingOneOf);
                oneOf.Add(new JsonSchemaBuilder()
                    .Ref(defName.ToJsonDefUri()));

                defs[ifaceDefName] = ifaceSchemaBuilder
                    .OneOf(oneOf);
            }
            else
            {
                ifaceSchemaBuilder = new JsonSchemaBuilder()
                    .AddTypeAnnotations(context, iface)
                    .OneOf(new JsonSchemaBuilder()
                        .Ref(defName.ToJsonDefUri()));

                defs.Add(ifaceDefName, ifaceSchemaBuilder);
            }
        }
    }
}
