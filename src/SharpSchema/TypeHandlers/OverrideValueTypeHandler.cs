// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Json.Schema;
using libanvl;
using SharpMeta;
using SharpSchema.Annotations;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles override value types.
/// </summary>
internal class OverrideValueTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(
        JsonSchemaBuilder builder,
        ConverterContext context,
        Type type,
        bool isRootType,
        Opt<PropertyInfo> propertyInfo)
    {
        if (!type.TryGetCustomAttributeData<SchemaOverrideAttribute>(out CustomAttributeData? attribute))
        {
            return Result.NotHandled(builder);
        }

        string definitionName = type.ToDefinitionName(context);
        if (context.Defs.TryGetValue(definitionName, out _))
        {
            return Result.Handled(builder
                .Ref(definitionName.ToJsonDefUri()));
        }

        try
        {
            string? overrideValue = attribute.GetConstructorArgument<string>(0);
            if (overrideValue is null)
            {
                return Result.Fault(builder, "Override value is null.");
            }

            JsonSchemaBuilder overrideBuilder = new();

            var overrideSchema = JsonSchema.FromText(overrideValue);
            foreach (IJsonSchemaKeyword keyword in overrideSchema.Keywords ?? Enumerable.Empty<IJsonSchemaKeyword>())
            {
                overrideBuilder.Add(keyword);
            }

            context.Defs.Add(definitionName, overrideBuilder);

            return Result.Handled(builder
                .Ref(definitionName.ToJsonDefUri()));
        }
        catch (JsonException ex)
        {
            return Result.NotHandled(builder, ex.Message);
        }
    }
}
