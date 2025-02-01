using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Utilities;
using SharpSchema.Generator.Model;
using Json.Schema;
using System.Text.Json.Nodes;
using System.Diagnostics.CodeAnalysis;
using Humanizer;
using System.Xml.Linq;
using SharpSchema.Annotations;

namespace SharpSchema.Generator;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Builder = JsonSchemaBuilder;
using Metadata = Model.Metadata;

public class DeclaredTypeSyntaxVisitor : CSharpSyntaxVisitor<Builder?>
{
    private readonly CachingDeclaredTypeSyntaxVisitor _cachingVisitor;

    public DeclaredTypeSyntaxVisitor(Compilation compilation)
    {
        _cachingVisitor = new(compilation);
    }

    public override Builder? Visit(SyntaxNode? node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter($"[ROOT] {node.Kind()}");
        return base.Visit(node);
    }

    public override Builder? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node);
    }

    public override Builder? VisitStructDeclaration(StructDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node);
    }

    public override Builder? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node);
    }

    private Builder? VisitTypeDeclaration(TypeDeclarationSyntax node)
    {
        Builder builder = CreateTypeSchema(node, _cachingVisitor);

        if (_cachingVisitor.GetCachedSchemas() is IReadOnlyDictionary<string, JsonSchema> defs)
        {
            builder.Defs(defs);
        }

        return builder;
    }

    internal static Builder CreateTypeSchema(TypeDeclarationSyntax node, CSharpSyntaxVisitor<Builder?> typeVisitor)
    {
        Throw.IfNullArgument(node);

        Builder builder = CommonSchemas.Object;

        var properties = new Dictionary<string, JsonSchema>();

        foreach (MemberDeclarationSyntax member in node.Members)
        {
            if (member is PropertyDeclarationSyntax property && typeVisitor.Visit(property) is Builder propertyBuilder)
            {
                properties[property.Identifier.Text.Camelize()] = propertyBuilder;
            }
        }

        // Collect primary-constructor parameters
        if (node.ParameterList is not null)
        {
            foreach (ParameterSyntax parameter in node.ParameterList.Parameters)
            {
                if (typeVisitor.Visit(parameter) is Builder paramBuilder)
                {
                    properties[parameter.Identifier.Text.Camelize()] = paramBuilder;
                }
            }
        }

        // Apply collected properties
        return builder.Properties(properties);
    }
}

internal class CachingDeclaredTypeSyntaxVisitor : CSharpSyntaxVisitor<Builder?>
{
    private const string IEnumerableMetadataName = "System.Collections.Generic.IEnumerable`1";
    private const string IDictionaryMetadataName = "System.Collections.Generic.IDictionary`2";
    private const string IReadOnlyDictionaryMetadataName = "System.Collections.Generic.IReadOnlyDictionary`2";

    private readonly Compilation _compilation;
    private readonly SemanticModelCache _semanticModelCache;
    private readonly Metadata.SymbolVisitor _metadataVisitor;
    private readonly Dictionary<string, Builder> _cachedTypeSchemas;

    private INamedTypeSymbol? _enumerableOfTSymbol;
    private INamedTypeSymbol? _dictionaryOfKVSymbol;
    private INamedTypeSymbol? _readOnlyDictionaryOfKVSymbol;

    public CachingDeclaredTypeSyntaxVisitor(Compilation compilation)
    {
        _compilation = compilation;
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

        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(node);
        if (semanticModel.GetDeclaredSymbol(node) is not IPropertySymbol propertySymbol)
            return CommonSchemas.UnsupportedObject;

        if (propertySymbol.TryGetConstructorArgument<SchemaOverrideAttribute, string>(0, out string? schemaString))
        {
            return new Builder()
                .Apply(JsonSchema.FromText(schemaString));
        }

        Metadata metadata = Throw.ForUnexpectedNull(_metadataVisitor.Visit(propertySymbol));

        return GetPropertySchema(node.Type, metadata, node.Initializer?.Value);
    }

    public override Builder? VisitParameter(ParameterSyntax node)
    {
        Throw.IfNullArgument(node);

        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(node);
        if (semanticModel.GetDeclaredSymbol(node) is not IParameterSymbol parameterSymbol)
            return CommonSchemas.UnsupportedObject;

        if (parameterSymbol.TryGetConstructorArgument<SchemaOverrideAttribute, string>(0, out string? schemaString))
        {
            return new Builder()
                .Apply(JsonSchema.FromText(schemaString));
        }

        Metadata metadata = Throw.ForUnexpectedNull(_metadataVisitor.Visit(parameterSymbol));

        TypeSyntax? typeSyntax = node.Type;
        if (typeSyntax is null)
            return CommonSchemas.UnsupportedObject;

        return GetPropertySchema(typeSyntax, metadata, node.Default?.Value);
    }

    public override Builder? VisitIdentifierName(IdentifierNameSyntax node)
    {
        Throw.IfNullArgument(node);

        using var trace = Tracer.Enter(node.Identifier.Text);

        TypeInfo typeInfo = _semanticModelCache.GetSemanticModel(node).GetTypeInfo(node);
        if (typeInfo.ConvertedType is not ITypeSymbol typeSymbol)
        {
            return CommonSchemas.UnsupportedObject;
        }

        return typeSymbol.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Select(declaration => declaration.Accept(this))
            .FirstOrDefault(schema => schema is not null)
            ?? CommonSchemas.UnsupportedObject;
    }

    public override Builder? VisitGenericName(GenericNameSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);

        if (node.IsUnboundGenericName)
            return CommonSchemas.UnsupportedObject;

        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(node);

        if (semanticModel.GetTypeInfo(node).Type is not ITypeSymbol boundTypeSymbol)
            return CommonSchemas.UnsupportedObject.Comment($"{node.Identifier.Text}: Node does not produce a type symbol.");

        if (!InitializeGenericTypeSymbols(out Builder? errorBuilder))
            return errorBuilder;

        // Check if the symbol is a dictionary
        INamedTypeSymbol? boundDictionarySymbol = boundTypeSymbol.ImplementsGenericInterface(
            _dictionaryOfKVSymbol,
            _readOnlyDictionaryOfKVSymbol);

        if (boundDictionarySymbol is not null)
        {
            ITypeSymbol keyTypeSymbol = boundDictionarySymbol.TypeArguments.First();
            if (keyTypeSymbol.GetSchemaValueType() != SchemaValueType.String)
            {
                return CommonSchemas.UnsupportedObject.Comment($"{node.Identifier.Text}: Key type must be a string.");
            }

            ITypeSymbol valueTypeSymbol = boundDictionarySymbol.TypeArguments.Last();
            if (!valueTypeSymbol.IsJsonDefinedType(out Builder? valueSchema))
            {
                valueSchema = valueTypeSymbol.FindTypeDeclaration()?.Accept(this);
                if (valueSchema is null)
                    return CommonSchemas.UnsupportedObject.Comment($"{node.Identifier.Text}: Could not find schema for value type.");
            }

            return CommonSchemas.Object.AdditionalProperties(valueSchema);
        }

        // Check if the symbol is an enumerable
        INamedTypeSymbol? boundEnumerableSymbol = boundTypeSymbol.AllInterfaces
            .FirstOrDefault(i => i.OriginalDefinition.Equals(_enumerableOfTSymbol, SymbolEqualityComparer.Default));

        if (boundEnumerableSymbol is not null)
        {
            ITypeSymbol elementTypeSymbol = boundEnumerableSymbol.TypeArguments.First();
            if (!elementTypeSymbol.IsJsonDefinedType(out Builder? elementSchema))
            {
                elementSchema = elementTypeSymbol.FindTypeDeclaration()?.Accept(this);

                if (elementSchema is null)
                    return CommonSchemas.UnsupportedObject.Comment($"{node.Identifier.Text}: Could not find schema for element type.");
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
            return CommonSchemas.UnsupportedObject;
        }

        if (typeSymbol.IsJsonDefinedType(out Builder? valueTypeSchema))
        {
            return valueTypeSchema;
        }

        return CommonSchemas.UnsupportedObject;
    }

    public override Builder? VisitArrayType(ArrayTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Kind().ToString());

        Builder? elementSchema = node.ElementType.Accept(this);
        if (elementSchema is null)
            return CommonSchemas.UnsupportedObject;

        return CommonSchemas.ArrayOf(elementSchema);
    }

    private Builder? VisitTypeDeclaration(TypeDeclarationSyntax node)
    {
        if (_semanticModelCache.GetSemanticModel(node).GetDeclaredSymbol(node) is not ITypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject;

        string typeId = typeSymbol.GetDefCacheKey();
        if (_cachedTypeSchemas.TryGetValue(typeId, out _))
        {
            return CommonSchemas.DefRef(typeId);
        }

        if (typeSymbol.TryGetConstructorArgument<SchemaOverrideAttribute, string>(0, out string? schemaString))
        {
            return _cachedTypeSchemas[typeId] = new Builder()
                .Apply(JsonSchema.FromText(schemaString));
        }

        Builder builder = DeclaredTypeSyntaxVisitor.CreateTypeSchema(node, this);
        _cachedTypeSchemas[typeId] = builder;

        return CommonSchemas.DefRef(typeId);
    }

    private Builder GetPropertySchema(TypeSyntax typeSyntax, Metadata metadata, ExpressionSyntax? defaultExpression)
    {
        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(typeSyntax);
        if (semanticModel.GetTypeInfo(typeSyntax).Type is not ITypeSymbol typeSymbol)
            return CommonSchemas.UnsupportedObject;

        Builder? propertyBuilder = null;

        // These kinds are never directly cached
        if (typeSyntax.Kind() is SyntaxKind.NullableType or SyntaxKind.ArrayType or SyntaxKind.PredefinedType)
            propertyBuilder = typeSyntax.Accept(this);

        string typeId = typeSymbol.GetDefCacheKey();
        if (propertyBuilder is null && _cachedTypeSchemas.TryGetValue(typeId, out _))
            propertyBuilder = CommonSchemas.DefRef(typeId);

        if (propertyBuilder is null && typeSyntax.Accept(this) is Builder builder)
            propertyBuilder = builder;

        if (propertyBuilder is null)
            return CommonSchemas.UnsupportedObject;

        propertyBuilder.Apply(metadata);

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
            errorBuilder = CommonSchemas.UnsupportedObject.Comment("Could not find symbol for IEnumerable<T>.");
            return false;
        }

        _dictionaryOfKVSymbol ??= _compilation.GetTypeByMetadataName(IDictionaryMetadataName);
        if (_dictionaryOfKVSymbol is null)
        {
            errorBuilder = CommonSchemas.UnsupportedObject.Comment("Could not find symbol for IDictionary<K, V>.");
            return false;
        }

        _readOnlyDictionaryOfKVSymbol ??= _compilation.GetTypeByMetadataName(IReadOnlyDictionaryMetadataName);
        if (_readOnlyDictionaryOfKVSymbol is null)
        {
            errorBuilder = CommonSchemas.UnsupportedObject.Comment("Could not find symbol for IReadOnlyDictionary<K, V>.");
            return false;
        }

        errorBuilder = null;
        return true;
    }
}
