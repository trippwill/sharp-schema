using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Overrides the default traversal option for a given type.
/// </summary>
[AttributeUsage(SchemaAttribute.SupportedTypes)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaTraversalModeAttribute(TraversalMode value) : SchemaAttribute
#else
internal class SchemaTraversalModeAttribute(TraversalMode value) : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets the traversal option for the type.
    /// </summary>
    public TraversalMode Value { get; } = value;
}
