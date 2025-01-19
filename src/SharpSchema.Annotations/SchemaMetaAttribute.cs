using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Represents a schema meta attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaMetaAttribute : SchemaAttribute
#else
internal class SchemaMetaAttribute : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets or sets the title of the schema.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the description of the schema.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the comment for the schema.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets an example for the schema.
    /// </summary>
    public string? Example { get; set; }

    /// <summary>
    /// Gets or sets the deprecation message for the schema.
    /// </summary>
    public string? Deprecated { get; set; }
}
