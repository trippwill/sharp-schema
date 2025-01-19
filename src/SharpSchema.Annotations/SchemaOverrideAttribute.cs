using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Overrides the schema.
/// </summary>
/// <remarks>
/// This attribute is used to specify a custom value for a schema.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaOverrideAttribute(string value) : SchemaAttribute
#else
internal class SchemaOverrideAttribute(string value) : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets the overridden value for the schema.
    /// </summary>
    public string Value { get; } = value;
}
