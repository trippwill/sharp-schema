namespace SharpSchema.Generator.Model;

/// <summary>
/// The root type information for producing a schema.
/// </summary>
/// <param name="RootType">The root type of the schema.</param>
/// <param name="Filename">The filename associated with the schema.</param>
/// <param name="Id">The identifier of the schema.</param>
/// <param name="CommonNamespace">The common namespace of the schema.</param>
public record SchemaRootInfo(
        SchemaMember.Object RootType,
        string? Filename,
        string? Id,
        string? CommonNamespace);
