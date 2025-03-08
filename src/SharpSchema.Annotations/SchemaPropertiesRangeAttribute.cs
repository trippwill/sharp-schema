using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Represents a range of properties allowed in a schema.
/// </summary>
/// <remarks>
/// This attribute can be used to specify the minimum and maximum number of properties allowed in a schema.
/// </remarks>
[AttributeUsage(SupportedTypes | SupportedMembers)]
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
class SchemaPropertiesRangeAttribute : SchemaAttribute
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
