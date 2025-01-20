namespace SharpSchema.Generator.Model;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Represents a member of a schema, which can be an object or a property.
/// </summary>
/// <param name="MemberData">The data associated with the schema member.</param>
/// <param name="Override">An optional override value for the schema member.</param>
public abstract partial record SchemaMember(SchemaMember.Data? MemberData, string? Override)
{
}
