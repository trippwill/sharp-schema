using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Overrides the default number mode for a given type.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Parameter)]
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
class SchemaNumberModeAttribute(NumberMode value) : SchemaAttribute
{
    /// <summary>
    /// Gets the number mode.
    /// </summary>
    public NumberMode Value { get; } = value;
}
