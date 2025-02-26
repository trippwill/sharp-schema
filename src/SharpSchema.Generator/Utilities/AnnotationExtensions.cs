using Json.Schema;
using Microsoft.CodeAnalysis;
using SharpSchema.Annotations;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

using Builder = JsonSchemaBuilder;

internal static class AnnotationExtensions
{
    /// <summary>
    /// Determines if the symbol is ignored for generation based on specific attributes.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>True if the symbol is ignored for generation; otherwise, false.</returns>
    public static bool IsIgnoredForGeneration(this ISymbol symbol)
    {
        return symbol.GetAttributeData<SchemaIgnoreAttribute>() is not null;
    }

    public static Builder? GetOverrideSchema(this ISymbol symbol)
    {
        if (symbol.GetAttributeData<SchemaOverrideAttribute>() is AttributeData data
            && data.GetConstructorArgument<string>(0) is string schemaString)
        {
            try
            {
                return new Builder()
                    .ApplySchema(JsonSchema.FromText(schemaString));
            }
            catch (System.Text.Json.JsonException ex)
            {
                return CommonSchemas.UnsupportedObject($"Failed to parse schema override: {ex.Message}");
            }
        }

        return null;
    }
}
