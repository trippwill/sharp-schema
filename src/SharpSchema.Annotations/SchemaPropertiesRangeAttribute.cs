using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Represents a range of properties allowed in a schema.
/// </summary>
/// <remarks>
/// This attribute can be used to specify the minimum and maximum number of properties allowed in a schema.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaPropertiesRangeAttribute : SchemaAttribute
#else
internal class SchemaPropertiesRangeAttribute : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets or sets the minimum number of properties allowed in a schema.
    /// </summary>
    public uint Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of properties allowed in a schema.
    /// </summary>
    public uint Max { get; set; }
}
