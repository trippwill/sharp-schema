using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Represents an attribute that specifies a value range for a schema.
/// </summary>
/// <remarks>
/// This attribute can be used to define the minimum and maximum values allowed for a property in a schema.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaValueRangeAttribute : SchemaAttribute
#else
internal class SchemaValueRangeAttribute : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets or sets the minimum value allowed for the property.
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum value allowed for the property.
    /// </summary>
    public double Max { get; set; }
}
