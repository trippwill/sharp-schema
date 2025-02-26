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

    public LeafSyntaxVisitor(Compilation compilation, SemanticModelCache semanticModelCache,  GeneratorOptions options)
    {
        _compilation = compilation;
        _options = options;
        _semanticModelCache = semanticModelCache;
        _metadataVisitor = new();
        _collectionResolver = new(compilation);
        _cachedTypeSchemas = [];
        _cachedAbstractSymbols = [];
    }

    internal Dictionary<string, Builder> CachedTypeSchemas => _cachedTypeSchemas;

    internal Dictionary<string, INamedTypeSymbol> CachedAbstractSymbols => _cachedAbstractSymbols;

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
            return CommonSchemas.UnsupportedObject(Unsupported.EnumMessage, node.Identifier);

        if (enumSymbol.GetOverrideSchema() is Builder overrideSchema)
            return overrideSchema;

        string cacheKey = enumSymbol.GetDefCacheKey();
        if (!_cachedTypeSchemas.TryGetValue(cacheKey, out Builder? cachedSchema))
        {
            MemberMeta metadata = Throw.ForUnexpectedNull(_metadataVisitor.Visit(enumSymbol));

            Builder builder = EnumSymbolVisitor.Instance.Visit(enumSymbol, _options) ??
                CommonSchemas.UnsupportedObject(Unsupported.EnumMessage, node.Identifier);

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
            return CommonSchemas.UnsupportedObject(Unsupported.IdentifierMessage, node.Identifier);

        return this.Visit(typeSymbol.FindDeclaringSyntax())
            ?? CommonSchemas.UnsupportedObject(Unsupported.DeclarationMessage, node.Identifier);
    }

    public override Builder? VisitGenericName(GenericNameSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);

        if (node.IsUnboundGenericName)
            return CommonSchemas.UnsupportedObject(Unsupported.UnboundGenericMessage, node.Identifier);

        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(node);

        if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol boundTypeSymbol)
            return CommonSchemas.UnsupportedObject(Unsupported.TypeSymbolMessage, node.Identifier.Text);

        Builder? elementSchema = null;
        var (kind, keyType, elementSymbol) = _collectionResolver.Resolve(boundTypeSymbol);
        if (kind is CollectionKind.Dictionary or CollectionKind.Array)
        {
            elementSchema = node.TypeArgumentList.Arguments.Last().Accept(this);
        }

        if (kind is CollectionKind.Dictionary)
        {
            if (elementSchema is null)
                return CommonSchemas.UnsupportedObject(Unsupported.DictionaryElementMessage, elementSymbol.Name);

            Builder builder = CommonSchemas.Object;

            if (keyType is not SchemaValueType.String)
            {
                switch (_options.DictionaryKeyMode)
                {
                    case DictionaryKeyMode.Skip:
                        return null;
                    case DictionaryKeyMode.Strict:
                        return CommonSchemas.UnsupportedObject(Unsupported.KeyTypeMessage, keyType);
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
            if (elementSchema is null)
                return CommonSchemas.UnsupportedObject(Unsupported.ArrayElementMessage, elementSymbol);

            return CommonSchemas.ArrayOf(elementSchema);
        }

        Builder? boundTypeBuilder = this.Visit(boundTypeSymbol.FindDeclaringSyntax<BaseTypeDeclarationSyntax>());
        if (boundTypeBuilder is null)
            return CommonSchemas.UnsupportedObject(Unsupported.GenericTypeMessage, node.Identifier);

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

        Builder? elementSchema = node.ElementType.Accept(this);
        if (elementSchema is null)
            return CommonSchemas.UnsupportedObject(Unsupported.NullableElementMessage, node.ElementType);

        return CommonSchemas.Nullable(elementSchema);
    }

    public override Builder? VisitPredefinedType(PredefinedTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Keyword.Text);

        if (node.Keyword.IsKind(SyntaxKind.StringKeyword))
            return CommonSchemas.String;

        if (node.Keyword.IsKind(SyntaxKind.BoolKeyword))
            return CommonSchemas.Boolean;

        if (_semanticModelCache.GetSemanticModel(node).GetTypeInfo(node).Type is not INamedTypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject(Unsupported.PredefinedTypeMessage, node);

        bool shouldCache = _options.NumberMode is NumberMode.StrictDefs;

        string cacheKey = typeSymbol.GetDefCacheKey();
        if (shouldCache && _cachedTypeSchemas.TryGetValue(cacheKey, out Builder? cachedSchema))
            return CommonSchemas.DefRef(cacheKey);

        if (!typeSymbol.IsJsonDefinedType(_options.NumberMode, out Builder? valueTypeSchema))
            valueTypeSchema = CommonSchemas.UnsupportedObject(Unsupported.PredefinedTypeMessage, node.Keyword.Text);

        if (shouldCache)
        {
            _cachedTypeSchemas[cacheKey] = valueTypeSchema;
            return CommonSchemas.DefRef(cacheKey);
        }

        return valueTypeSchema;
    }

    public override Builder? VisitArrayType(ArrayTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Kind().ToString());

        Builder? elementSchema = node.ElementType.Accept(this);
        if (elementSchema is null)
            return CommonSchemas.UnsupportedObject(Unsupported.ArrayElementMessage, node.ElementType);

        return CommonSchemas.ArrayOf(elementSchema);
    }

    public Builder CreateTypeSchema(TypeDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);

        if (node.GetDeclaredSymbol(_semanticModelCache) is not INamedTypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject(Unsupported.TypeMessage, node.Identifier);

        return this.CreateTypeSchema(typeSymbol, node);
    }

    public Builder CreateTypeSchema(INamedTypeSymbol symbol, TypeDeclarationSyntax node, TraversalMode? traversalMode = null)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);

        return symbol.Accept(
            new NamedTypeSymbolVisitor( // Always create a new visitor instance
                this,
                _semanticModelCache,
                _options),
            argument: null)
            ?? CommonSchemas.UnsupportedObject(symbol.Name);
    }

    private Builder? VisitTypeDeclaration(TypeDeclarationSyntax node, Tracer.TraceScope trace)
    {
        if (_semanticModelCache.GetSemanticModel(node).GetDeclaredSymbol(node) is not INamedTypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject(Unsupported.SymbolMessage, node.Identifier.ValueText);

        string typeId = typeSymbol.GetDefCacheKey();
        if (_cachedTypeSchemas.TryGetValue(typeId, out _))
            return CommonSchemas.DefRef(typeId);

        if (typeSymbol.GetOverrideSchema() is Builder overrideSchema)
            return overrideSchema;

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
