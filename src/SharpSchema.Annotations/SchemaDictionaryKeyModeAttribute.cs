using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Overrides the default dictionary key mode for a given type.
/// </summary>
[AttributeUsage(SchemaAttribute.SupportedMembers)]
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
class SchemaDictionaryKeyModeAttribute(DictionaryKeyMode value) : SchemaAttribute
{
    /// <summary>
    /// Gets the dictionary key mode.
    /// </summary>
    public DictionaryKeyMode Value { get; } = value;
}
