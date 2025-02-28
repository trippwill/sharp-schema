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

    public static JsonSchemaBuilder MergeProperties(this JsonSchemaBuilder @base, JsonSchema apply)
    {
        using var trace = Tracer.Enter($"{apply.BaseUri}");

        IReadOnlyDictionary<string, JsonSchema>? baseProperties = @base.Get<PropertiesKeyword>()?.Properties;
        IReadOnlyDictionary<string, JsonSchema>? applyProperties = apply.GetProperties();

        if (applyProperties is null)
            return @base;

        Dictionary<string, JsonSchema> properties = new(StringComparer.OrdinalIgnoreCase);
        if (baseProperties is not null)
        {
            foreach ((string name, JsonSchema value) in baseProperties)
                properties.Add(name, value);
        }

        if (apply.GetProperties() is IReadOnlyDictionary<string, JsonSchema> props)
        {
            foreach ((string name, JsonSchema applyValue) in props)
            {
                if (properties.TryGetValue(name, out JsonSchema? baseValue))
                    properties[name] = baseValue.ApplySchema(applyValue);
                else
                    properties.Add(name, applyValue);
            }
        }

        return properties.Count > 0 ? @base.Properties(properties) : @base;
    }

    public static JsonSchemaBuilder UnsupportedObject(this JsonSchemaBuilder builder, string value)
    {
        builder.Add(new UnsupportedObjectKeyword(value));
        return builder;
    }
}
