using System.Text.Json.Nodes;
using Json.Schema;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

internal static class JsonSchemaBuilderExtensions
{
    public static JsonSchemaBuilder ApplyMetadata(this JsonSchemaBuilder builder, Metadata? data)
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

    public static JsonSchemaBuilder MergeBaseProperties(this JsonSchemaBuilder builder, JsonSchema baseSchema)
    {
        using var trace = Tracer.Enter($"{baseSchema.BaseUri}");

        IReadOnlyDictionary<string, JsonSchema> baseProperties = builder.Get<PropertiesKeyword>()?.Properties ?? new Dictionary<string, JsonSchema>();
        IReadOnlyDictionary<string, JsonSchema> otherProperties = baseSchema.GetProperties() ?? new Dictionary<string, JsonSchema>();

        Dictionary<string, JsonSchema> mergedProperties = new((IDictionary<string, JsonSchema>)otherProperties, StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, JsonSchema> pair in baseProperties)
        {
            if (mergedProperties.TryGetValue(pair.Key, out JsonSchema? value))
                mergedProperties[pair.Key] = pair.Value.ApplySchema(value);
            else
                mergedProperties.Add(pair.Key, pair.Value);
        }

        // Merge required properties
        IReadOnlyList<string> baseRequiredProperties = builder.Get<RequiredKeyword>()?.Properties ?? [];
        IReadOnlyList<string> otherRequiredProperties = baseSchema.GetRequired() ?? [];

        HashSet<string> mergedRequiredProperties = new(baseRequiredProperties, StringComparer.OrdinalIgnoreCase);
        foreach (string requiredProperty in otherRequiredProperties)
            mergedRequiredProperties.Add(requiredProperty);

        return builder
            .Properties(mergedProperties)
            .Required(mergedRequiredProperties);
    }

    public static JsonSchemaBuilder UnsupportedObject(this JsonSchemaBuilder builder, string value)
    {
        builder.Add(new UnsupportedObjectKeyword(value));
        return builder;
    }

    public static UnsupportedObjectKeyword? GetUnsupportedObject(this JsonSchemaBuilder builder)
    {
        return builder.Get<UnsupportedObjectKeyword>();
    }
}
