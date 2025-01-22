using Microsoft.CodeAnalysis;

namespace SharpSchema.Generator.Model;

/// <summary>
/// Represents a node of a schema tree, which can be an object or a property.
/// </summary>
/// <param name="Symbol">The symbol associated with the schema node.</param>
/// <param name="Metadata">The metadata associated with the schema node.</param>
public abstract partial record SchemaNode(ISymbol Symbol, Metadata? Metadata)
{
}
