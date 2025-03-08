using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Overrides the schema.
/// </summary>
/// <remarks>
/// This attribute is used to specify a custom value for a schema.
/// </remarks>
[AttributeUsage(SupportedTypes | SupportedMembers | EnumTargets)]
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
class SchemaOverrideAttribute(string value) : SchemaAttribute
{
    /// <summary>
    /// Gets the overridden value for the schema.
    /// </summary>
    public string Value { get; } = value;
}
