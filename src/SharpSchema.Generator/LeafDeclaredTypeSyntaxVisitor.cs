using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Utilities;
using SharpSchema.Generator.Model;
using Json.Schema;
using System.Text.Json.Nodes;
using System.Diagnostics.CodeAnalysis;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;
using Metadata = Model.Metadata;

internal class LeafDeclaredTypeSyntaxVisitor : CSharpSyntaxVisitor<Builder?>
{
    private const string IEnumerableMetadataName = "System.Collections.Generic.IEnumerable`1";
    private const string IDictionaryMetadataName = "System.Collections.Generic.IDictionary`2";
    private const string IReadOnlyDictionaryMetadataName = "System.Collections.Generic.IReadOnlyDictionary`2";

    private readonly Compilation _compilation;
    private readonly GeneratorOptions _options;
    private readonly SemanticModelCache _semanticModelCache;
    private readonly Metadata.SymbolVisitor _metadataVisitor;
    private readonly Dictionary<string, Builder> _cachedTypeSchemas;

    private INamedTypeSymbol? _enumerableOfTSymbol;
    private INamedTypeSymbol? _dictionaryOfKVSymbol;
    private INamedTypeSymbol? _readOnlyDictionaryOfKVSymbol;

    public LeafDeclaredTypeSyntaxVisitor(Compilation compilation, GeneratorOptions options)
    {
        _compilation = compilation;
        _options = options;
        _semanticModelCache = new(compilation);
        _metadataVisitor = new();
        _cachedTypeSchemas = [];
    }

    public IReadOnlyDictionary<string, JsonSchema>? GetCachedSchemas()
    {
        if (_cachedTypeSchemas.Count == 0)
            return null;

        return _cachedTypeSchemas.ToDictionary(
            p => string.Format(CommonSchemas.DefUriFormat, p.Key),
            p => p.Value.Build());
    }

    public override Builder? Visit(SyntaxNode? node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter($"[LEAF] {node.Kind()}");
        return base.Visit(node);
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

    public override Builder? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);

        if (node.GetDeclaredSymbol(_semanticModelCache) is not IPropertySymbol propertySymbol)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for property '{node.Identifier}'.");

        if (!propertySymbol.ShouldProcess(_options))
            return null;

        if (propertySymbol.GetOverrideSchema() is Builder overrideSchema)
            return overrideSchema;

        Metadata metadata = Throw.ForUnexpectedNull(_metadataVisitor.Visit(propertySymbol));

        return GetPropertySchema(node.Type, metadata, node.Initializer?.Value);
    }

    public override Builder? VisitParameter(ParameterSyntax node)
    {
        Throw.IfNullArgument(node);

        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(node);
        if (semanticModel.GetDeclaredSymbol(node) is not IParameterSymbol parameterSymbol)
            return CommonSchemas.UnsupportedObject($"Failed to build schema for parameter '{node.Identifier}'.");

        if (!parameterSymbol.ShouldProcess(_options))
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

        return typeSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Select(declaration => declaration.Accept(this))
            .FirstOrDefault(schema => schema is not null)
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

        trace.WriteLine("Initializing generic type symbols.");
        if (!InitializeGenericTypeSymbols(out Builder? errorBuilder))
            return errorBuilder;

        // Check if the symbol is a dictionary
        INamedTypeSymbol? boundDictionarySymbol = boundTypeSymbol.ImplementsGenericInterface(
            _dictionaryOfKVSymbol,
            _readOnlyDictionaryOfKVSymbol);

        if (boundDictionarySymbol is not null)
        {
            trace.WriteLine("Dictionary type found.");

            Builder builder = CommonSchemas.Object;

            ITypeSymbol keyTypeSymbol = boundDictionarySymbol.TypeArguments.First();
            if (keyTypeSymbol.GetSchemaValueType() != SchemaValueType.String)
            {
                trace.WriteLine($"Key type is not string. Using DictionaryKeyMode: {_options.DictionaryKeyMode}");

                switch (_options.DictionaryKeyMode)
                {
                    case DictionaryKeyMode.Loose:
                        builder.Comment($"Key type '{keyTypeSymbol.Name}' must be convertible to string.");
                        break;
                    case DictionaryKeyMode.Strict:
                        return CommonSchemas.UnsupportedObject($"Key type '{keyTypeSymbol.Name}' must be string.");
                    case DictionaryKeyMode.Skip:
                        return new Builder();
                }
            }

            ITypeSymbol valueTypeSymbol = boundDictionarySymbol.TypeArguments.Last();
            if (!valueTypeSymbol.IsJsonDefinedType(out Builder? valueSchema))
            {
                valueSchema = valueTypeSymbol.FindTypeDeclaration()?.Accept(this);
                if (valueSchema is null)
                    return CommonSchemas.UnsupportedObject($"Could not find schema for value type of '{node.Identifier.Text}'.");
            }

            return builder.AdditionalProperties(valueSchema);
        }

        // Check if the symbol is an enumerable
        INamedTypeSymbol? boundEnumerableSymbol = boundTypeSymbol.ImplementsGenericInterface(_enumerableOfTSymbol);

        if (boundEnumerableSymbol is not null)
        {
            trace.WriteLine("Enumerable type found.");

            ITypeSymbol elementTypeSymbol = boundEnumerableSymbol.TypeArguments.First();
            if (!elementTypeSymbol.IsJsonDefinedType(out Builder? elementSchema))
            {
                elementSchema = elementTypeSymbol.FindTypeDeclaration()?.Accept(this);

                if (elementSchema is null)
                    return CommonSchemas.UnsupportedObject($"Could not find schema for element type of '{node.Identifier.Text}'.");
            }

            return CommonSchemas.ArrayOf(elementSchema);
        }

        return boundTypeSymbol.FindTypeDeclaration()?.Accept(this);
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
        if (_semanticModelCache.GetSemanticModel(node).GetDeclaredSymbol(node) is not ITypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject($"Failed building schema for {node.Identifier.ValueText}");

        string typeId = typeSymbol.GetDefCacheKey();
        if (_cachedTypeSchemas.TryGetValue(typeId, out _))
        {
            return CommonSchemas.DefRef(typeId);
        }

        if (typeSymbol.GetOverrideSchema() is Builder overrideSchema)
        {
            return overrideSchema;
        }

        Builder builder = node.CreateTypeSchema(this);
        _cachedTypeSchemas[typeId] = builder;

        return CommonSchemas.DefRef(typeId);
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

    [MemberNotNullWhen(true, nameof(_enumerableOfTSymbol), nameof(_dictionaryOfKVSymbol), nameof(_readOnlyDictionaryOfKVSymbol))]
    private bool InitializeGenericTypeSymbols([NotNullWhen(false)] out Builder? errorBuilder)
    {
        _enumerableOfTSymbol ??= _compilation.GetTypeByMetadataName(IEnumerableMetadataName);
        if (_enumerableOfTSymbol is null)
        {
            errorBuilder = CommonSchemas.UnsupportedObject("Could not find symbol for 'IEnumerable<T>'.");
            return false;
        }

        _dictionaryOfKVSymbol ??= _compilation.GetTypeByMetadataName(IDictionaryMetadataName);
        if (_dictionaryOfKVSymbol is null)
        {
            errorBuilder = CommonSchemas.UnsupportedObject("Could not find symbol for 'IDictionary<K, V>'.");
            return false;
        }

        _readOnlyDictionaryOfKVSymbol ??= _compilation.GetTypeByMetadataName(IReadOnlyDictionaryMetadataName);
        if (_readOnlyDictionaryOfKVSymbol is null)
        {
            errorBuilder = CommonSchemas.UnsupportedObject("Could not find symbol for 'IReadOnlyDictionary<K, V>'.");
            return false;
        }

        errorBuilder = null;
        return true;
    }
}
