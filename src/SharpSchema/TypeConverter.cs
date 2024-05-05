// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Json.Schema;

namespace SharpSchema;

/// <summary>
/// Converts a <see cref="RootTypeContext"/> to a <see cref="JsonSchemaBuilder"/>.
/// </summary>
/// <param name="options">The converter options.</param>
public class TypeConverter(TypeConverter.Options? options = null)
{
    /// <summary>
    /// Converts a <see cref="RootTypeContext"/> to a <see cref="JsonSchemaBuilder"/>.
    /// </summary>
    /// <param name="typeContext">The root type context to convert.</param>
    /// <returns>The converted <see cref="JsonSchemaBuilder"/>.</returns>
    public JsonSchemaBuilder Convert(RootTypeContext typeContext)
    {
        options ??= Options.Default;

        JsonSchemaBuilder builder = new JsonSchemaBuilder()
            .Schema("http://json-schema.org/draft-07/schema#");

        if (typeContext.Id is string id)
        {
            builder = builder.Id(id);
        }

        ConverterContext converterContext = new()
        {
            IncludeInterfaces = options.IncludeInterfaces,
            EnumAsUnderlyingType = options.EnumAsUnderlyingType,
            MaxDepth = options.MaxDepth,
            DefaultNamespace = typeContext.CommonNamespace,
        };

        builder = builder.AddType(typeContext.Type, converterContext, isRootType: true);
        if (converterContext.Defs.Count > 0)
        {
            builder = builder.Defs(converterContext.Defs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Build()));
        }

        return builder;
    }

    /// <summary>
    /// Options for the <see cref="TypeConverter"/>.
    /// </summary>
    public record class Options(bool IncludeInterfaces = false, bool EnumAsUnderlyingType = false, int MaxDepth = 50)
    {
        /// <summary>
        /// Gets the default options.
        /// </summary>
        public static Options Default { get; } = new();
    }
}
