using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Overrides the default traversal option for a given type.
/// </summary>
[AttributeUsage(SchemaAttribute.SupportedMembers)]
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
class SchemaAccessibilityModeAttribute(AccessibilityMode value) : SchemaAttribute
{
    /// <summary>
    /// Gets the accessibility option for the type.
    /// </summary>
    public AccessibilityMode Value { get; } = value;
}
