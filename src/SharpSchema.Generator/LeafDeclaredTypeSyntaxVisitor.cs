using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Utilities;
using SharpSchema.Generator.Model;
using Json.Schema;
using System.Text.Json.Nodes;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;
using Metadata = Model.Metadata;

internal class LeafDeclaredTypeSyntaxVisitor : CSharpSyntaxVisitor<Builder?>
{
    private readonly Compilation _compilation;
    private readonly GeneratorOptions _options;
    private readonly SemanticModelCache _semanticModelCache;
    private readonly Metadata.SymbolVisitor _metadataVisitor;
    private readonly CollectionSymbolVisitor _collectionVisitor;
    private readonly Dictionary<string, Builder> _cachedTypeSchemas;
    private readonly Dictionary<string, Builder> _cachedBaseSchemas;

    public LeafDeclaredTypeSyntaxVisitor(Compilation compilation, GeneratorOptions options)
    {
        _compilation = compilation;
        _options = options;
        _semanticModelCache = new(compilation);
        _metadataVisitor = new();
        _collectionVisitor = new(options, compilation, this);
        _cachedTypeSchemas = [];
        _cachedBaseSchemas = [];
    }

    internal GeneratorOptions Options => _options;

    public IReadOnlyDictionary<string, JsonSchema>? GetCachedSchemas()
    {
        if (_cachedTypeSchemas.Count == 0)
            return null;

        return _cachedTypeSchemas.ToDictionary(
            p => p.Key,
            p => p.Value.Build());
    }

    public override Builder? Visit(SyntaxNode? node)
    {
        if (node is null)
            return null;

        using var trace = Tracer.Enter($"[LEAF] {node.Kind()}");
        return base.Visit(node);
    }

    public override Builder? VisitQualifiedName(QualifiedNameSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.ToString());

        TypeInfo typeInfo = _semanticModelCache.GetSemanticModel(node).GetTypeInfo(node);
        if (typeInfo.ConvertedType is not ITypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for qualified name '{node}'.");

        return this.Visit(typeSymbol.FindBaseTypeDeclaration());
    }

    public override Builder? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return VisitTypeDeclaration(node);
    }

    public override Builder? VisitStructDeclaration(StructDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return VisitTypeDeclaration(node);
    }

    public override Builder? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return VisitTypeDeclaration(node);
    }

    public override Builder? VisitSimpleBaseType(SimpleBaseTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Type.ToString());
        if (node.Type is not IdentifierNameSyntax nameSyntax)
            return CommonSchemas.UnsupportedObject($"The base type syntax is not supported for generation '{node.Type}'.");

        if (_semanticModelCache.GetSemanticModel(node).GetTypeInfo(node.Type).Type is not INamedTypeSymbol baseTypeSymbol)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for base type '{node.Type}'.");

        string cacheKey = baseTypeSymbol.GetDefCacheKey();
        if (!_cachedBaseSchemas.TryGetValue(cacheKey, out Builder? cachedSchema))
        {
            cachedSchema = baseTypeSymbol.FindTypeDeclaration()?.CreateTypeSchema(this);
            if (cachedSchema is not null)
            {
                _cachedBaseSchemas[cacheKey] = cachedSchema;
            }
        }

        return cachedSchema;
    }

    public override Builder? VisitEnumDeclaration(EnumDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);

        if (node.GetDeclaredSymbol(_semanticModelCache) is not INamedTypeSymbol enumSymbol)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for enum '{node.Identifier}'.");

        if (enumSymbol.GetOverrideSchema() is Builder overrideSchema)
            return overrideSchema;

        string cacheKey = enumSymbol.GetDefCacheKey();
        if (!_cachedTypeSchemas.TryGetValue(cacheKey, out Builder? cachedSchema))
        {

            Metadata metadata = Throw.ForUnexpectedNull(_metadataVisitor.Visit(enumSymbol));

            Builder builder = EnumSymbolVisitor.Instance.Visit(enumSymbol, _options) ??
                CommonSchemas.UnsupportedObject($"Failed to build schema for enum '{node.Identifier}'.");

            _cachedTypeSchemas[cacheKey] = builder.ApplyMetadata(metadata);
        }

        return CommonSchemas.DefRef(cacheKey);
    }

    public override Builder? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);

        using var trace = Tracer.Enter(node.Identifier.Text);

        if (node.GetDeclaredSymbol(_semanticModelCache) is not IPropertySymbol propertySymbol)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for property '{node.Identifier}'.");

        if (!_options.ShouldProcess(propertySymbol))
            return null;

        if (propertySymbol.GetOverrideSchema() is Builder overrideSchema)
            return overrideSchema;

        Metadata metadata = Throw.ForUnexpectedNull(_metadataVisitor.Visit(propertySymbol));

        return GetPropertySchema(node.Type, metadata, node.Initializer?.Value);
    }

    public override Builder? VisitParameter(ParameterSyntax node)
    {
        Throw.IfNullArgument(node);

        using var trace = Tracer.Enter(node.Identifier.Text);

        if (node.GetDeclaredSymbol(_semanticModelCache) is not IParameterSymbol parameterSymbol)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for parameter '{node.Identifier}'.");

        if (!_options.ShouldProcess(parameterSymbol))
            return null;

        if (parameterSymbol.GetOverrideSchema() is Builder overrideSchema)
            return overrideSchema;

        Metadata metadata = Throw.ForUnexpectedNull(_metadataVisitor.Visit(parameterSymbol));

        TypeSyntax? typeSyntax = node.Type;
        if (typeSyntax is null)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for parameter type '{typeSyntax}'.");

        return GetPropertySchema(typeSyntax, metadata, node.Default?.Value);
    }

    public override Builder? VisitIdentifierName(IdentifierNameSyntax node)
    {
        Throw.IfNullArgument(node);

        using var trace = Tracer.Enter(node.Identifier.Text);

        TypeInfo typeInfo = _semanticModelCache.GetSemanticModel(node).GetTypeInfo(node);
        if (typeInfo.ConvertedType is not ITypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for identifier '{node.Identifier}'.");

        return this.Visit(typeSymbol.FindBaseTypeDeclaration())
            ?? CommonSchemas.UnsupportedObject($"Could not find declaration for identifier '{node.Identifier}'.");
    }

    public override Builder? VisitGenericName(GenericNameSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);

        if (node.IsUnboundGenericName)
            return CommonSchemas.UnsupportedObject($"Failed to evaluate unbound generic type '{node.Identifier}'.");

        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(node);

        if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol boundTypeSymbol)
            return CommonSchemas.UnsupportedObject($"Node '{node.Identifier.Text}' does not produce a type symbol.");

        Builder? collectionBuilder = _collectionVisitor.Visit(boundTypeSymbol);
        if (collectionBuilder is not null)
            return collectionBuilder;

        return this.Visit(boundTypeSymbol.FindTypeDeclaration());
    }

    public override Builder? VisitNullableType(NullableTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Kind().ToString());

        return CommonSchemas.Nullable(
            Throw.ForUnexpectedNull(
                node.ElementType.Accept(this)));
    }

    public override Builder? VisitPredefinedType(PredefinedTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Keyword.Text);

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
        using var trace = Tracer.Enter(node.Kind().ToString());

        Builder? elementSchema = node.ElementType.Accept(this);
        if (elementSchema is null)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for array element type '{node.ElementType}'.");

        return CommonSchemas.ArrayOf(elementSchema);
    }

    private Builder? VisitTypeDeclaration(TypeDeclarationSyntax node)
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
            List<JsonSchema> oneOfBuilders = [];
            foreach (INamedTypeSymbol concrete in GetConcreteSubtypes(typeSymbol))
            {
                TypeDeclarationSyntax? decl = concrete.FindTypeDeclaration();
                if (decl is not null)
                {
                    var subtypeBuilder = this.Visit(decl);
                    if (subtypeBuilder is not null)
                        oneOfBuilders.Add(subtypeBuilder);
                }
            }

            if (oneOfBuilders.Count > 0)
            {
                Builder abstractBuilder = CommonSchemas.Object; // Create a new def builder.
                abstractBuilder = abstractBuilder.OneOf([.. oneOfBuilders]);
                _cachedTypeSchemas[typeId] = abstractBuilder;

                return CommonSchemas.DefRef(typeId);
            }
        }

        // Regular concrete type.
        Builder builder = node.CreateTypeSchema(this);

        _cachedTypeSchemas[typeId] = builder;
        return CommonSchemas.DefRef(typeId);
    }

    // Helper to get all concrete subtypes of an abstract class.
    private IEnumerable<INamedTypeSymbol> GetConcreteSubtypes(INamedTypeSymbol abstractType)
    {
        using var trace = Tracer.Enter(abstractType.Name);
        foreach (SyntaxTree tree in _compilation.SyntaxTrees)
        {
            SemanticModel model = _compilation.GetSemanticModel(tree);
            foreach (var node in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(node) is not INamedTypeSymbol symbol)
                    continue;

                if (!symbol.IsAbstract && symbol.InheritsFrom(abstractType))
                {
                    trace.WriteLine($"Found concrete subtype: {symbol.Name}");
                    yield return symbol;
                }
            }
        }
    }

    private Builder GetPropertySchema(TypeSyntax typeSyntax, Metadata metadata, ExpressionSyntax? defaultExpression)
    {
        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(typeSyntax);
        if (semanticModel.GetTypeInfo(typeSyntax).Type is not ITypeSymbol typeSymbol || !typeSymbol.IsValidForGeneration())
            return CommonSchemas.UnsupportedObject($"Failed to build schema for type '{typeSyntax}'.");

        Builder? propertyBuilder = null;

        // These kinds are never directly cached
        if (typeSyntax.Kind() is SyntaxKind.NullableType or SyntaxKind.ArrayType or SyntaxKind.PredefinedType)
            propertyBuilder = this.Visit(typeSyntax);

        string typeId = typeSymbol.GetDefCacheKey();
        if (propertyBuilder is null && _cachedTypeSchemas.TryGetValue(typeId, out _))
            propertyBuilder = CommonSchemas.DefRef(typeId);

        if (propertyBuilder is null && this.Visit(typeSyntax) is Builder builder)
            propertyBuilder = builder;

        if (propertyBuilder is null)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for type '{typeSymbol.Name}'.");

        propertyBuilder.ApplyMetadata(metadata);

        JsonNode? defaultValue = null;
        if (defaultExpression is not null && semanticModel.GetConstantValue(defaultExpression) is { HasValue: true } constantValue)
        {
            defaultValue = JsonValue.Create(constantValue.Value);
        }

        return defaultValue is not null
            ? propertyBuilder.Default(defaultValue)
            : propertyBuilder;
    }
}
