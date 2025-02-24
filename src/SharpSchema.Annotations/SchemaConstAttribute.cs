using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Specifies a constant value for a property or field in a schema.
/// </summary>
[AttributeUsage(SupportedMembers)]
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
class SchemaConstAttribute(object value) : SchemaAttribute
{
    /// <summary>
    /// Gets the constant value.
    /// </summary>
    public object Value { get; } = value;
}
