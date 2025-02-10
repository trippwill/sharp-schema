using Json.Schema;

namespace SharpSchema.Generator.Model;

/// <summary>
/// The root type information for producing a schema.
/// </summary>
/// <param name="Schema">The schema associated with the root type.</param>
/// <param name="Filename">The filename associated with the schema.</param>
/// <param name="Id">The identifier of the schema.</param>
/// <param name="CommonNamespace">The common namespace of the schema.</param>
public record SchemaTree(
        JsonSchema Schema,
        string? Filename,
        string? Id,
        string? CommonNamespace);
