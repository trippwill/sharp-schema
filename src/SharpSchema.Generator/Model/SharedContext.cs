using Json.Schema;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Resolvers;

namespace SharpSchema.Generator.Model;

internal record RootContext(
    Compilation Compilation,
    SemanticModelCache SemanticModelCache,
    CollectionResolver CollectionResolver,
    Dictionary<string, JsonSchemaBuilder> CachedTypeSchemas,
    Dictionary<string, INamedTypeSymbol> CachedAbstractSymbols)
{
    public RootContext(
        Compilation compilation,
        SemanticModelCache semanticModelCache) : this(
            compilation,
            semanticModelCache,
            new(compilation),
            new(StringComparer.OrdinalIgnoreCase),
            new(StringComparer.OrdinalIgnoreCase))
    {
    }
}
