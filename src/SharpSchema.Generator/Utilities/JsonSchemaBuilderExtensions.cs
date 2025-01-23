using System.Text.Json.Nodes;
using Json.Schema;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

internal static class JsonSchemaBuilderExtensions
{
    public static JsonSchemaBuilder Apply(this JsonSchemaBuilder builder, Metadata? data)
    {
        if (data is null)
            return builder;

        builder.Title(data.Title);

        if (data.Description is string description)
            builder.Description(description);

        if (data.Deprecated)
            builder.Deprecated(true);

        if (data.Examples is List<string> examples && examples.Count > 0)
            builder.Examples([.. examples.Select(e => JsonValue.Create(e))]);

        if (data.Comment is string comment)
            builder.Comment(comment);

        return builder;
    }

    public static JsonSchemaBuilder Apply(this JsonSchemaBuilder builder, JsonSchema schema)
    {
        foreach (IJsonSchemaKeyword keyword in schema.Keywords ?? [])
            builder.Add(keyword);

        return builder;
    }
}
