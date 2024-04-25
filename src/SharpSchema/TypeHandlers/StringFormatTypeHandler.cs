// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Json.Schema;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles string format types.
/// </summary>
internal class StringFormatTypeHandler : TypeHandler
{
    private static readonly Dictionary<string, Func<JsonSchemaBuilder, JsonSchemaBuilder>> StringFormatSchemas = new(capacity: 5)
    {
        [typeof(Guid).Name] = (builder) => builder
            .Comment("Guid")
            .Type(SchemaValueType.String)
            .Format(Formats.Uuid),

        [typeof(Uri).Name] = (builder) => builder
            .Comment("Uri")
            .Type(SchemaValueType.String)
            .Format(Formats.Uri),

        [typeof(DateTimeOffset).Name] = (builder) => builder
            .Comment("DateTimeOffset")
            .Type(SchemaValueType.String)
            .Format(Formats.DateTime),

        [typeof(TimeOnly).Name] = (builder) => builder
            .Comment("TimeOnly")
            .Type(SchemaValueType.String)
            .Format(Formats.Time),

        [typeof(DateOnly).Name] = (builder) => builder
            .Comment("DateOnly")
            .Type(SchemaValueType.String)
            .Format(Formats.Date),
    };

    /// <inheritdoc/>
    public override Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        // handle specific types with string formats
        if (!StringFormatSchemas.TryGetValue(type.Name, out Func<JsonSchemaBuilder, JsonSchemaBuilder>? stringFormatSchema))
        {
            return Result.NotHandled(builder);
        }

        builder = stringFormatSchema(builder);
        return Result.Handled(builder);
    }
}
