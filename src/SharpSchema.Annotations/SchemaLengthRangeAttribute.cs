using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Represents an attribute that specifies the length range for a schema.
/// </summary>
/// <remarks>
/// This attribute can be used to define the minimum and maximum length of a schema.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaLengthRangeAttribute : SchemaAttribute
#else
internal class SchemaLengthRangeAttribute : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets or sets the minimum length allowed for the schema.
    /// </summary>
    public uint Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum length allowed for the schema.
    /// </summary>
    public uint Max { get; set; }
}
