using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Applies a format to a schema property.
/// </summary>
/// <remarks>
/// This attribute is used to specify the format of a schema.
/// </remarks>
[AttributeUsage(SupportedMembers)]
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
class SchemaFormatAttribute(string format) : SchemaAttribute
{
    /// <summary>
    /// Gets the format of the schema.
    /// </summary>
    public string Format { get; } = format;
}
