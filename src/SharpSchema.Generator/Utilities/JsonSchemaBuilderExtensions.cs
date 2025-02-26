using System.Text.Json.Nodes;
using Json.Schema;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

internal static class JsonSchemaBuilderExtensions
{
    public static JsonSchemaBuilder ApplyMemberMeta(this JsonSchemaBuilder builder, MemberMeta? data)
    {
        using var scope = Tracer.Enter($"{data}");

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

    public static JsonSchemaBuilder ApplySchema(this JsonSchemaBuilder @base, JsonSchema apply)
    {
        using var trace = Tracer.Enter($"{apply.BaseUri}");

        foreach (IJsonSchemaKeyword keyword in apply.Keywords ?? [])
            @base.Add(keyword);

        return @base;
    }

    public static JsonSchemaBuilder ApplySchema(this JsonSchema @base, JsonSchema apply)
    {
        using var trace = Tracer.Enter($"{apply.BaseUri}");

        JsonSchemaBuilder builder = new JsonSchemaBuilder().ApplySchema(@base);
        foreach (IJsonSchemaKeyword keyword in apply.Keywords ?? [])
            builder.Add(keyword);

        return builder;
    }

    public static JsonSchemaBuilder UnsupportedObject(this JsonSchemaBuilder builder, string value)
    {
        builder.Add(new UnsupportedObjectKeyword(value));
        return builder;
    }
}
