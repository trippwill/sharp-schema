using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
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
    private readonly RootContext _context;
    private readonly GeneratorOptions _options;

    public LeafSyntaxVisitor(RootContext context, GeneratorOptions options)
    {
        _context = context;
        _options = options;
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

        if (node.GetDeclaredSymbol(_context.SemanticModelCache) is not INamedTypeSymbol enumSymbol)
            return CommonSchemas.UnsupportedObject(Unsupported.EnumMessage, node.Identifier);

        if (enumSymbol.GetOverrideSchema() is Builder overrideSchema)
            return overrideSchema;

        string cacheKey = enumSymbol.GetDefCacheKey();
        if (_context.CachedTypeSchemas.TryGetValue(cacheKey, out Builder? cachedSchema))
            return CommonSchemas.DefRef(cacheKey);

        MemberMeta metadata = Throw.IfUnexpectedNull(MemberMeta.SymbolVisitor.Default.Visit(enumSymbol));

        Builder builder = EnumResolver.Resolve(enumSymbol, _options) ??
            CommonSchemas.UnsupportedObject(Unsupported.EnumMessage, node.Identifier);

        _context.CachedTypeSchemas[cacheKey] = builder.ApplyMemberMeta(metadata);

        return CommonSchemas.DefRef(cacheKey);
    }

    public override Builder? VisitIdentifierName(IdentifierNameSyntax node)
    {
        Throw.IfNullArgument(node);

        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);

        TypeInfo typeInfo = node.GetTypeInfo(_context.SemanticModelCache);
        if (typeInfo.ConvertedType is not ITypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject(Unsupported.IdentifierMessage, node.Identifier);

        if (typeSymbol.FindDeclaringSyntax() is SyntaxNode syntaxNode)
            return this.Visit(syntaxNode);

        return null;
    }

    public override Builder? VisitGenericName(GenericNameSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);

        if (node.IsUnboundGenericName)
            return CommonSchemas.UnsupportedObject(Unsupported.UnboundGenericMessage, node.Identifier);

        if (node.GetTypeInfo(_context.SemanticModelCache).Type is not INamedTypeSymbol boundTypeSymbol)
            return CommonSchemas.UnsupportedObject(Unsupported.TypeSymbolMessage, node.Identifier.Text);

        if (boundTypeSymbol.HasUnresolvedTypeArguments())
        {
            trace.WriteLine("Unresolved type arguments found.");
            return null;
        }

        var (kind, keyType, elementSymbol) = _context.CollectionResolver.Resolve(boundTypeSymbol);

        return kind switch
        {
            CollectionKind.Dictionary => HandleDictionaryType(node, keyType, elementSymbol),
            CollectionKind.Array => HandleArrayType(node, elementSymbol),
            _ => HandleGenericType(node, boundTypeSymbol)
        };

        // -- Local functions --

        Builder? HandleDictionaryType(GenericNameSyntax node, SchemaValueType keyType, ITypeSymbol elementSymbol)
        {
            if (node.TypeArgumentList.Arguments[^1].Accept(this) is not Builder elementSchema)
                return CommonSchemas.UnsupportedObject(Unsupported.DictionaryElementMessage, elementSymbol.Name);

            if (keyType is SchemaValueType.String)
                return CommonSchemas.Object.AdditionalProperties(elementSchema);

            return _options.DictionaryKeyMode switch
            {
                DictionaryKeyMode.Skip => null,
                DictionaryKeyMode.Strict => CommonSchemas.UnsupportedObject(Unsupported.KeyTypeMessage, keyType),
                DictionaryKeyMode.Loose => CommonSchemas.Object
                    .Comment($"Key type '{keyType}' must be convertible to string")
                    .AdditionalProperties(elementSchema),
                DictionaryKeyMode.Silent => CommonSchemas.Object
                    .AdditionalProperties(elementSchema),
                _ => Throw.UnknownEnumValue<Builder?>(_options.DictionaryKeyMode)
            };
        }

        Builder? HandleArrayType(GenericNameSyntax node, ITypeSymbol elementSymbol)
        {
            Builder? elementSchema = node.TypeArgumentList.Arguments[^1].Accept(this);
            if (elementSchema is null)
                return CommonSchemas.UnsupportedObject(Unsupported.ArrayElementMessage, elementSymbol);

            return CommonSchemas.ArrayOf(elementSchema);
        }

        Builder? HandleGenericType(GenericNameSyntax node, INamedTypeSymbol boundTypeSymbol)
        {
            Builder? boundTypeBuilder = Visit(boundTypeSymbol.FindDeclaringSyntax<BaseTypeDeclarationSyntax>());
            if (boundTypeBuilder is null)
                return CommonSchemas.UnsupportedObject(Unsupported.GenericTypeMessage, node.Identifier);

            // Add to the oneOf for the unbound generic type
            INamedTypeSymbol unboundGeneric = boundTypeSymbol.ConstructUnboundGenericType();
            string cacheKey = unboundGeneric.GetDefCacheKey();

            if (_context.CachedTypeSchemas.TryGetValue(cacheKey, out Builder? cachedSchema) &&
                cachedSchema.Get<OneOfKeyword>()?.Schemas is IReadOnlyList<JsonSchema> currentOneOf)
            {
                _context.CachedTypeSchemas[cacheKey] = cachedSchema.OneOf(currentOneOf.Append(boundTypeBuilder));
            }
            else if (_context.CachedTypeSchemas.TryGetValue(cacheKey, out cachedSchema))
            {
                _context.CachedTypeSchemas[cacheKey] = CommonSchemas.Object.OneOf(cachedSchema, boundTypeBuilder);
            }

            return boundTypeBuilder;
        }
    }

    public override Builder? VisitNullableType(NullableTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Kind().ToString());

        if (node.ElementType.Accept(this) is Builder elementSchema)
            return CommonSchemas.Nullable(elementSchema);

        return null;
    }

    public override Builder? VisitPredefinedType(PredefinedTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Keyword.Text);

        if (node.Keyword.IsKind(SyntaxKind.StringKeyword))
            return CommonSchemas.String;

        if (node.Keyword.IsKind(SyntaxKind.BoolKeyword))
            return CommonSchemas.Boolean;

        if (_context.SemanticModelCache.GetSemanticModel(node).GetTypeInfo(node).Type is not INamedTypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject(Unsupported.PredefinedTypeMessage, node);

        bool shouldCache = _options.NumberMode is NumberMode.StrictDefs;
        string cacheKey = typeSymbol.GetDefCacheKey();

        if (shouldCache && _context.CachedTypeSchemas.TryGetValue(cacheKey, out Builder? cachedSchema))
            return CommonSchemas.DefRef(cacheKey);

        if (!typeSymbol.IsJsonDefinedType(_options.NumberMode, out Builder? valueTypeSchema))
            valueTypeSchema = CommonSchemas.UnsupportedObject(Unsupported.PredefinedTypeMessage, node.Keyword.Text);

        if (shouldCache)
        {
            _context.CachedTypeSchemas[cacheKey] = valueTypeSchema;
            return CommonSchemas.DefRef(cacheKey);
        }

        return valueTypeSchema;
    }

    public override Builder? VisitArrayType(ArrayTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Kind().ToString());

        if (node.ElementType.Accept(this) is Builder elementSchema)
            return CommonSchemas.ArrayOf(elementSchema);

        return null;
    }

    public override Builder? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);

        Builder? typeBuilder = node.Type.Accept(this);
        if (typeBuilder is null)
            return null;

        if (node.ExpressionBody is ArrowExpressionClauseSyntax aec
            && GetConstantValue(aec.Expression) is JsonNode constantValue)
        {
            typeBuilder = CommonSchemas.Const(constantValue);
        }
        else if (ExtractDefaultValue(node) is JsonNode defaultValue)
        {
            typeBuilder = typeBuilder.Default(defaultValue);
        }

        return typeBuilder;

        // -- Local functions --

        JsonNode? ExtractDefaultValue(PropertyDeclarationSyntax propertyDeclaration)
        {
            return propertyDeclaration.Initializer is EqualsValueClauseSyntax evc
                ? GetConstantValue(evc.Value)
                : null;
        }

        JsonNode? GetConstantValue(SyntaxNode node)
        {
            SemanticModel sm = _context.SemanticModelCache.GetSemanticModel(node);
            return sm.GetConstantValue(node) is Optional<object?> optValue
                && optValue.HasValue
                ? JsonValue.Create(optValue.Value)
                : (JsonNode?)null;
        }
    }

    /// <summary>
    /// Creates a type schema for the specified type declaration syntax.
    /// </summary>
    /// <param name="node">The type declaration syntax node.</param>
    /// <returns>A JSON schema builder for the type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    public Builder CreateTypeSchema(TypeDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);

        if (node.GetDeclaredSymbol(_context.SemanticModelCache) is not INamedTypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject(Unsupported.TypeMessage, node.Identifier);

        return this.CreateTypeSchema(typeSymbol, node);
    }

    /// <summary>
    /// Creates a type schema for the specified symbol and declaration syntax.
    /// </summary>
    /// <param name="symbol">The named type symbol.</param>
    /// <param name="node">The type declaration syntax node.</param>
    /// <param name="traversalMode">Optional traversal mode override.</param>
    /// <returns>A JSON schema builder for the type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when node is null.</exception>
    public Builder CreateTypeSchema(INamedTypeSymbol symbol, TypeDeclarationSyntax node, TraversalMode? traversalMode = null)
    {
        Throw.IfNullArgument(node);
        using Tracer.TraceScope trace = Tracer.Enter(node.Identifier.Text);

        NamedTypeResolver visitor = new(_context);
        return visitor.Resolve(symbol, _options) ?? CommonSchemas.UnsupportedObject(symbol.Name);
    }

    private Builder? VisitTypeDeclaration(TypeDeclarationSyntax node, Tracer.TraceScope trace)
    {
        if (node.GetDeclaredSymbol(_context.SemanticModelCache) is not INamedTypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject(Unsupported.SymbolMessage, node.Identifier.ValueText);

        string typeId = typeSymbol.GetDefCacheKey();

        if (_context.CachedTypeSchemas.TryGetValue(typeId, out _))
            return CommonSchemas.DefRef(typeId);

        if (typeSymbol.GetOverrideSchema() is Builder overrideSchema)
            return overrideSchema;

        if (typeSymbol.IsAbstract)
        {
            trace.WriteLine($"Found abstract type '{typeSymbol.Name}'.");
            if (!_context.CachedAbstractSymbols.ContainsKey(typeId))
            {
                _context.CachedAbstractSymbols.Add(typeId, typeSymbol);
            }

            return CommonSchemas.DefRef(typeId);
        }

        AttributeHandler schemaTraversal = typeSymbol.GetAttributeHandler<SchemaTraversalModeAttribute>();
        TraversalMode effectiveTraversalMode = schemaTraversal.Get<TraversalMode>(0) ?? _options.TraversalMode;

        // Regular concrete type.
        Builder builder = CreateTypeSchema(typeSymbol, node, effectiveTraversalMode);

        _context.CachedTypeSchemas[typeId] = builder;
        return CommonSchemas.DefRef(typeId);
    }
}
