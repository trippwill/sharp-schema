using System.Collections.Immutable;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;

/// <summary>
/// Visits C# syntax nodes to generate JSON schema builders.
/// </summary>
public class RootSyntaxVisitor : CSharpSyntaxVisitor<Builder?>
{
    private readonly LeafSyntaxVisitor _cachingVisitor;
    private readonly Compilation _compilation;
    private readonly SemanticModelCache _semanticModelCache;

    /// <summary>
    /// Initializes a new instance of the <see cref="RootSyntaxVisitor"/> class.
    /// </summary>
    /// <param name="compilation">The compilation context.</param>
    /// <param name="options">The generator options.</param>
    public RootSyntaxVisitor(Compilation compilation, GeneratorOptions options)
    {
        Throw.IfNullArgument(compilation);
        Throw.IfNullArgument(options);

        _compilation = compilation;
        _semanticModelCache = new(compilation);
        _cachingVisitor = new(compilation, _semanticModelCache, options);
    }

    /// <inheritdoc />
    /// <summary>
    /// Visits a syntax node.
    /// </summary>
    /// <param name="node">The syntax node to visit.</param>
    /// <returns>A JSON schema builder or null.</returns>
    public override Builder? Visit(SyntaxNode? node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter($"[ROOT] {node.Kind()}");
        Builder? builder = base.Visit(node);

        Builder result = new Builder()
            .Schema("http://json-schema.org/draft-07/schema#");

        if (builder is not null)
        {
            result.ApplySchema(builder);
        }

        return result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Visits a class declaration syntax node.
    /// </summary>
    /// <param name="node">The class declaration syntax node to visit.</param>
    /// <returns>A JSON schema builder or null.</returns>
    public override Builder? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node);
    }

    /// <inheritdoc />
    /// <summary>
    /// Visits a struct declaration syntax node.
    /// </summary>
    /// <param name="node">The struct declaration syntax node to visit.</param>
    /// <returns>A JSON schema builder or null.</returns>
    public override Builder? VisitStructDeclaration(StructDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node);
    }

    /// <inheritdoc />
    /// <summary>
    /// Visits a record declaration syntax node.
    /// </summary>
    /// <param name="node">The record declaration syntax node to visit.</param>
    /// <returns>A JSON schema builder or null.</returns>
    public override Builder? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node);
    }

    /// <summary>
    /// Visits a type declaration syntax node.
    /// </summary>
    /// <param name="node">The type declaration syntax node to visit.</param>
    /// <returns>A JSON schema builder or null.</returns>
    private Builder? VisitTypeDeclaration(TypeDeclarationSyntax node)
    {
        Builder builder = _cachingVisitor.CreateTypeSchema(node);

        Dictionary<string, INamedTypeSymbol> cachedAbstractSymbols = _cachingVisitor.CachedAbstractSymbols;
        Dictionary<string, Builder> cachedTypeSchemas = _cachingVisitor.CachedTypeSchemas;

        if (cachedAbstractSymbols.Count > 0)
        {
            using var trace = Tracer.Enter("Building abstract type schemas.");
            ImmutableArray<NamedType> namedTypes = [.. _compilation.GetAllNamedTypes(_semanticModelCache)];

            foreach ((string key, INamedTypeSymbol abstractSymbol) in cachedAbstractSymbols)
            {
                trace.WriteLine($"Building schema for abstract type '{abstractSymbol.Name}'.");

                IEnumerable<NamedType> subTypes = namedTypes
                    .Where(t => t.Symbol.InheritsFrom(abstractSymbol));

                List<JsonSchema> subSchemas = [];

                foreach (NamedType subType in subTypes)
                {
                    // Check if the sub-type is already cached
                    string cacheKey = subType.Symbol.GetDefCacheKey();
                    if (cachedTypeSchemas.TryGetValue(cacheKey, out Builder? cachedSchema))
                    {
                        trace.WriteLine($"Using cached schema for '{subType.Symbol.Name}'.");
                        subSchemas.Add(CommonSchemas.DefRef(cacheKey));
                    }
                    else
                    {
                        trace.WriteLine($"Building schema for '{subType.Symbol.Name}'.");
                        Builder? subSchema = _cachingVisitor.CreateTypeSchema(subType.Symbol, subType.SyntaxNode);
                        if (subSchema is not null)
                        {
                            cachedTypeSchemas[cacheKey] = subSchema;
                            subSchemas.Add(CommonSchemas.DefRef(cacheKey));
                        }
                    }
                }

                if (subSchemas.Count > 0)
                {
                    trace.WriteLine($"Found {subSchemas.Count} sub-types for '{abstractSymbol.Name}'.");
                    Builder abstractSchema = CommonSchemas.Object.OneOf(subSchemas);
                    cachedTypeSchemas[key] = abstractSchema;
                }
                else
                    trace.WriteLine($"No sub-types found for '{abstractSymbol.Name}'.");
            }
        }

        if (cachedTypeSchemas.Count > 0)
        {
            builder.Defs(cachedTypeSchemas.ToDictionary(
            p => p.Key,
            p => p.Value.Build()));
        }

        return builder;
    }
}
