using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Represents a base class for schema attributes.
/// </summary>
#if SHARPSCHEMA_ASSEMBLY
public abstract class SchemaAttribute : Attribute
#else
internal abstract class SchemaAttribute : Attribute
#endif
{
    internal const AttributeTargets SupportedTypes = AttributeTargets.Class | AttributeTargets.Struct;

    internal const AttributeTargets SupportedMembers = AttributeTargets.Property | AttributeTargets.Parameter;

    internal const AttributeTargets EnumTargets = AttributeTargets.Field | AttributeTargets.Enum;
}
