using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Overrides the default traversal option for a given type.
/// </summary>
[AttributeUsage(SchemaAttribute.SupportedMembers)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaAccessibilityModeAttribute(AccessibilityMode value) : SchemaAttribute
#else
internal class SchemaAccessibilityModeAttribute(AccessibilityMode value) : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets the accessibility option for the type.
    /// </summary>
    public AccessibilityMode Value { get; } = value;
}
