using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Indicates whether a property is required in a schema.
/// </summary>
[AttributeUsage(SupportedMembers)]
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
class SchemaRequiredAttribute(bool isRequired = true) : SchemaAttribute
{
    /// <summary>
    /// Gets a value indicating whether the property is required.
    /// </summary>
    public bool IsRequired { get; } = isRequired;
}
