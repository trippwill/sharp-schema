using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Specifies the value of an enum member in a schema.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
class SchemaEnumValueAttribute(string value) : SchemaAttribute
{
    /// <summary>
    /// Gets the value of the enum member.
    /// </summary>
    public string Value { get; } = value;
}
