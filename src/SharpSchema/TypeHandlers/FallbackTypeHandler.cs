// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Json.Schema;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles types that do not have a specific type handler.
/// </summary>
internal class FallbackTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        try
        {
            builder = AddObjectType(builder, type, isRootType, context, propertyAttributeData);
            return Result.Handled(builder);
        }
        catch (Exception ex)
        {
            return Result.Fault(builder, ex.Message);
        }
    }

    private static JsonSchemaBuilder AddObjectType(JsonSchemaBuilder builder, Type type, bool isTopLevel, ConverterContext context, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        // handle other generics
        if (type.IsGenericType)
        {
            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            Type[] genericArguments = type.GetGenericArguments();

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

        return AddComplexType(builder, type, context, isTopLevel);
    }

    private static JsonSchemaBuilder AddComplexType(JsonSchemaBuilder builder, Type type, ConverterContext context, bool isTopLevel)
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
}
