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
public class SchemaFormatAttribute(string format) : SchemaAttribute
#else
internal class SchemaFormatAttribute(string format) : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets the format of the schema.
    /// </summary>
    public string Format { get; } = format;
}
