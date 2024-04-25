// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;
using Json.Schema;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles ambient value types.
/// </summary>
internal class AmbientValueTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        if (!type.TryGetAmbientValue(out string? ambientValue) || ambientValue is null)
        {
            return Result.NotHandled(builder);
        }

        try
        {
            var ambientSchema = JsonSchema.FromText(ambientValue);
            foreach (IJsonSchemaKeyword keyword in ambientSchema.Keywords ?? Enumerable.Empty<IJsonSchemaKeyword>())
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
