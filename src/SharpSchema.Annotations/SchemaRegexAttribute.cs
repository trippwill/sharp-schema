using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Represents a regular expression attribute for schema validation.
/// </summary>
/// <remarks>
/// This attribute is used to specify a regular expression pattern that a property value must match in order to be considered valid.
/// </remarks>
[AttributeUsage(SupportedMembers)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaRegexAttribute(string pattern) : SchemaAttribute
#else
internal class SchemaRegexAttribute(string pattern) : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets the regular expression pattern.
    /// </summary>
    public string Pattern { get; } = pattern;

    /// <summary>
    /// Gets or sets a value indicating whether the regular expression pattern should be applied to the property name.
    /// </summary>
    public bool ApplyToPropertyName { get; set; }
}
