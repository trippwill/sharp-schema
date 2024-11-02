// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Json.Schema;
using SharpMeta;
using SharpSchema.Annotations;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles override value types.
/// </summary>
internal class OverrideValueTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        if (!type.TryGetCustomAttributeData<SchemaOverrideAttribute>(out CustomAttributeData? attribute))
        {
            return Result.NotHandled(builder);
        }

        try
        {
            string? overrideValue = attribute.GetConstructorArgument<string>(0);
            if (overrideValue is null)
            {
                return Result.Fault(builder, "Override value is null.");
            }

            var overrideSchema = JsonSchema.FromText(overrideValue);
            foreach (IJsonSchemaKeyword keyword in overrideSchema.Keywords ?? Enumerable.Empty<IJsonSchemaKeyword>())
            {
                builder.Add(keyword);
            }

            return Result.Handled(builder);
        }
        catch (JsonException ex)
        {
            return Result.NotHandled(builder, ex.Message);
        }
    }
}
