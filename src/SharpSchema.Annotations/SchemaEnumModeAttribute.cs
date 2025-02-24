using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Overrides the default enum mode for a given type.
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
class SchemaEnumModeAttribute(EnumMode value) : SchemaAttribute
{
    /// <summary>
    /// Gets the enum mode.
    /// </summary>
    public EnumMode Value { get; } = value;
}
