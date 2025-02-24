using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Annotations;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Resolvers;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;

internal partial class LeafSyntaxVisitor : CSharpSyntaxVisitor<Builder?>
{
    private readonly Compilation _compilation;
    private readonly GeneratorOptions _options;
    private readonly SemanticModelCache _semanticModelCache;
    private readonly MemberMeta.SymbolVisitor _metadataVisitor;
    private readonly CollectionResolver _collectionResolver;
    private readonly Dictionary<string, Builder> _cachedTypeSchemas;
    private readonly Dictionary<string, INamedTypeSymbol> _cachedAbstractSymbols;

    public LeafSyntaxVisitor(Compilation compilation, GeneratorOptions options)
    {
        _compilation = compilation;
        _options = options;
        _semanticModelCache = new(compilation);
        _metadataVisitor = new();
        _collectionResolver = new(compilation);
        _cachedTypeSchemas = [];
        _cachedAbstractSymbols = [];
    }

    internal GeneratorOptions Options => _options;

    public IReadOnlyDictionary<string, JsonSchema>? GetCachedSchemas()
    {
        if (_cachedAbstractSymbols.Count > 0)
        {
            using var trace = Tracer.Enter("Building abstract type schemas.");
            ImmutableArray<NamedType> namedTypes = [.. _compilation.GetAllNamedTypes(_semanticModelCache)];

            foreach ((string key, INamedTypeSymbol abstractSymbol) in _cachedAbstractSymbols)
            {
                trace.WriteLine($"Building schema for abstract type '{abstractSymbol.Name}'.");

                IEnumerable<NamedType> subTypes = namedTypes
                    .Where(t => t.Symbol.InheritsFrom(abstractSymbol));

                List<JsonSchema> subSchemas = [];

                foreach (NamedType subType in subTypes)
                {
                    // Check if the sub-type is already cached
                    string cacheKey = subType.Symbol.GetDefCacheKey();
                    if (_cachedTypeSchemas.TryGetValue(cacheKey, out Builder? cachedSchema))
                    {
                        trace.WriteLine($"Using cached schema for '{subType.Symbol.Name}'.");
                        subSchemas.Add(CommonSchemas.DefRef(cacheKey));
                    }
                    else
                    {
                        trace.WriteLine($"Building schema for '{subType.Symbol.Name}'.");
                        Builder? subSchema = this.CreateTypeSchema(subType.Symbol, subType.SyntaxNode);
                        if (subSchema is not null)
                        {
                            _cachedTypeSchemas[cacheKey] = subSchema;
                            subSchemas.Add(CommonSchemas.DefRef(cacheKey));
                        }
                    }
                }

                if (subSchemas.Count > 0)
                {
                    trace.WriteLine($"Found {subSchemas.Count} sub-types for '{abstractSymbol.Name}'.");
                    Builder abstractSchema = CommonSchemas.Object.OneOf(subSchemas);
                    _cachedTypeSchemas[key] = abstractSchema;
                }
                else
                    trace.WriteLine($"No sub-types found for '{abstractSymbol.Name}'.");
            }
        }

        if (_cachedTypeSchemas.Count == 0)
            return null;

        return _cachedTypeSchemas.ToDictionary(
            p => p.Key,
            p => p.Value.Build());
    }

    [ExcludeFromCodeCoverage]
    public override Builder? DefaultVisit(SyntaxNode node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope scope = Tracer.Enter($"{node.Kind()} {node.GetLocation().GetLineSpan().StartLinePosition.Line}");
        return null;
    }

    public override Builder? VisitQualifiedName(QualifiedNameSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Right.Identifier.Text);
        return this.Visit(node.Right);
    }

    public override Builder? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node, trace);
    }

    public override Builder? VisitStructDeclaration(StructDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node, trace);
    }

    public override Builder? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node, trace);
    }

    public override Builder? VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);

        if (node.GetDeclaredSymbol(_semanticModelCache) is not INamedTypeSymbol enumSymbol)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for enum '{node.Identifier}'.");

        if (enumSymbol.GetOverrideSchema() is Builder overrideSchema)
            return overrideSchema;

        string cacheKey = enumSymbol.GetDefCacheKey();
        if (!_cachedTypeSchemas.TryGetValue(cacheKey, out Builder? cachedSchema))
        {
            MemberMeta metadata = Throw.ForUnexpectedNull(_metadataVisitor.Visit(enumSymbol));

            Builder builder = EnumSymbolVisitor.Instance.Visit(enumSymbol, _options) ??
                CommonSchemas.UnsupportedObject($"Failed to build schema for enum '{node.Identifier}'.");

            _cachedTypeSchemas[cacheKey] = builder.ApplyMemberMeta(metadata);
        }

        return CommonSchemas.DefRef(cacheKey);
    }

    public override Builder? VisitIdentifierName(IdentifierNameSyntax node)
    {
        Throw.IfNullArgument(node);

        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);

        TypeInfo typeInfo = node.GetTypeInfo(_semanticModelCache);
        if (typeInfo.ConvertedType is not ITypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for identifier '{node.Identifier}'.");

        return this.Visit(typeSymbol.FindDeclaringSyntax())
            ?? CommonSchemas.UnsupportedObject($"Could not find declaration for identifier '{node.Identifier}'.");
    }

    public override Builder? VisitGenericName(GenericNameSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);

        if (node.IsUnboundGenericName)
            return CommonSchemas.UnsupportedObject($"Failed to evaluate unbound generic type '{node.Identifier}'.");

        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(node);

        if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol boundTypeSymbol)
            return CommonSchemas.UnsupportedObject($"Node '{node.Identifier.Text}' does not produce a type symbol.");

        var (kind, keyType, elementSymbol) = _collectionResolver.Resolve(boundTypeSymbol);
        if (kind is CollectionKind.Dictionary)
        {
            if (!elementSymbol.IsJsonDefinedType(out Builder? elementSchema))
            {
                if (elementSymbol.FindDeclaringSyntax() is BaseTypeDeclarationSyntax elx)
                    elementSchema = elx.Accept(this);
            }

            if (elementSchema is null)
                return CommonSchemas.UnsupportedObject($"Failed to build schema for dictionary element type '{elementSymbol}'.");

            Builder builder = CommonSchemas.Object;

            if (keyType is not SchemaValueType.String)
            {
                switch (_options.DictionaryKeyMode)
                {
                    case DictionaryKeyMode.Skip:
                        return null;
                    case DictionaryKeyMode.Strict:
                        return CommonSchemas.UnsupportedObject($"Key type '{keyType}' is not supported.");
                    case DictionaryKeyMode.Loose:
                        builder = builder.Comment($"Key type '{keyType}' must be convertible to string");
                        break;
                    case DictionaryKeyMode.Silent:
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected dictionary key mode '{_options.DictionaryKeyMode}'.");
                }
            }

            return builder.AdditionalProperties(elementSchema);
        }

        if (kind is CollectionKind.Array)
        {
            if (!elementSymbol.IsJsonDefinedType(out Builder? elementSchema))
            {
                if (elementSymbol.FindDeclaringSyntax() is BaseTypeDeclarationSyntax elx)
                    elementSchema = elx.Accept(this);
            }

            if (elementSchema is null)
                return CommonSchemas.UnsupportedObject($"Failed to build schema for array element type '{elementSymbol}'.");

            return CommonSchemas.ArrayOf(elementSchema);
        }

        Builder? boundTypeBuilder = this.Visit(boundTypeSymbol.FindDeclaringSyntax<BaseTypeDeclarationSyntax>());
        if (boundTypeBuilder is null)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for generic type '{node.Identifier}'.");

        // Add to the oneOf for the unbound generic type
        INamedTypeSymbol unboundGeneric = boundTypeSymbol.ConstructUnboundGenericType();
        string cacheKey = unboundGeneric.GetDefCacheKey();
        if (_cachedTypeSchemas.TryGetValue(cacheKey, out Builder? cachedSchema))
        {
            IReadOnlyList<JsonSchema>? currentOneOf = cachedSchema.Get<OneOfKeyword>()?.Schemas;
            cachedSchema = currentOneOf is not null
                ? cachedSchema.OneOf(currentOneOf.Append(boundTypeBuilder))
                : CommonSchemas.Object.OneOf(cachedSchema, boundTypeBuilder);
        }

        return boundTypeBuilder;
    }

    public override Builder? VisitNullableType(NullableTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Kind().ToString());

        Builder? elementSchema = this.Visit(node.ElementType);
        return elementSchema is null
            ? new Builder().Const(null)
            : CommonSchemas.Nullable(
                Throw.ForUnexpectedNull(
                    node.ElementType.Accept(this)));
    }

    public override Builder? VisitPredefinedType(PredefinedTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Keyword.Text);

        if (_semanticModelCache.GetSemanticModel(node).GetTypeInfo(node).Type is not INamedTypeSymbol typeSymbol)
        {
            return CommonSchemas.UnsupportedObject($"Failed to build schema for predefined type '{node}'.");
        }

        if (typeSymbol.IsJsonDefinedType(out Builder? valueTypeSchema))
        {
            return valueTypeSchema;
        }

        return CommonSchemas.UnsupportedObject($"Unexpected built-in type '{node.Keyword.Text}'.");
    }

    public override Builder? VisitArrayType(ArrayTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Kind().ToString());

        Builder? elementSchema = node.ElementType.Accept(this);
        if (elementSchema is null)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for array element type '{node.ElementType}'.");

        return CommonSchemas.ArrayOf(elementSchema);
    }

    public Builder CreateTypeSchema(TypeDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);

        if (node.GetDeclaredSymbol(_semanticModelCache) is not INamedTypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for type '{node.Identifier}'.");

        return this.CreateTypeSchema(typeSymbol, node);
    }

    private Builder CreateTypeSchema(INamedTypeSymbol symbol, TypeDeclarationSyntax node, TraversalMode? traversalMode = null)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);

        return symbol.Accept(
            new TypeSchemaSymbolVisitor(
                this,
                _semanticModelCache,
                _options),
            node)
            ?? CommonSchemas.UnsupportedObject(symbol.Name);
    }

    private Builder? VisitTypeDeclaration(TypeDeclarationSyntax node, Tracer.TraceScope trace)
    {
        if (_semanticModelCache.GetSemanticModel(node).GetDeclaredSymbol(node) is not INamedTypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject($"Failed building schema for {node.Identifier.ValueText}");

        string typeId = typeSymbol.GetDefCacheKey();
        if (_cachedTypeSchemas.TryGetValue(typeId, out _))
            return CommonSchemas.DefRef(typeId);

        if (typeSymbol.GetOverrideSchema() is Builder overrideSchema)
            return overrideSchema;

        // Handle abstract types
        if (typeSymbol.IsAbstract)
        {
            trace.WriteLine($"Found abstract type '{typeSymbol.Name}'.");
            if (!_cachedAbstractSymbols.TryGetValue(typeId, out _))
            {
                _cachedAbstractSymbols.Add(typeId, typeSymbol);
            }

            return CommonSchemas.DefRef(typeId);
        }

        AttributeHandler schemaTraversal = typeSymbol.GetAttributeHandler<SchemaTraversalModeAttribute>();

        // Regular concrete type.
        Builder builder = CreateTypeSchema(typeSymbol, node, schemaTraversal.Get<TraversalMode>(0) ?? _options.TraversalMode);

        _cachedTypeSchemas[typeId] = builder;
        return CommonSchemas.DefRef(typeId);
    }
}
