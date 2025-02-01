using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Utilities;
using SharpSchema.Generator.Model;
using Json.Schema;
using System.Text.Json.Nodes;
using System.Diagnostics.CodeAnalysis;

namespace SharpSchema.Generator;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using Builder = JsonSchemaBuilder;
using Metadata = Model.Metadata;

public class DeclaredTypeSyntaxVisitor : CSharpSyntaxVisitor<Builder?>
{
    private const string IEnumerableMetadataName = "System.Collections.Generic.IEnumerable`1";
    private const string IDictionaryMetadataName = "System.Collections.Generic.IDictionary`2";
    private const string IReadOnlyDictionaryMetadataName = "System.Collections.Generic.IReadOnlyDictionary`2";

    private readonly SemanticModelCache _semanticModelCache;
    private readonly Metadata.SymbolVisitor _metadataVisitor;
    private readonly Compilation _compilation;

    private INamedTypeSymbol? _enumerableOfTSymbol;
    private INamedTypeSymbol? _dictionaryOfKVSymbol;
    private INamedTypeSymbol? _readOnlyDictionaryOfKVSymbol;

    public DeclaredTypeSyntaxVisitor(Compilation compilation)
    {
        _semanticModelCache = new(compilation);
        _metadataVisitor = new();
        _compilation = compilation;
    }

    public override Builder? Visit(SyntaxNode? node)
    {
        Throw.IfNullArgument(node);
        using var trace = TraceScope.Enter($"{node.Kind()}");
        return base.Visit(node);
    }

    public override Builder? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = TraceScope.Enter(node.Identifier.Text);
        return VisitTypeDeclaration(node);
    }

    public override Builder? VisitStructDeclaration(StructDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = TraceScope.Enter(node.Identifier.Text);
        return VisitTypeDeclaration(node);
    }

    public override Builder? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = TraceScope.Enter(node.Identifier.Text);
        return VisitTypeDeclaration(node);
    }

    public override Builder? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);

        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(node);
        if (semanticModel.GetDeclaredSymbol(node) is not IPropertySymbol propertySymbol)
            return CommonSchemas.UnsupportedObject;

        Metadata metadata = Throw.ForUnexpectedNull(_metadataVisitor.Visit(propertySymbol));

        JsonNode? defaultValue = null;
        if (node.Initializer?.Value is ExpressionSyntax initializer)
        {
            if (semanticModel.GetConstantValue(initializer) is { HasValue: true } constantValue)
            {
                defaultValue = JsonValue.Create(constantValue.Value);
            }
        }

        return GetPropertySchema(node.Type, metadata, defaultValue);
    }

    public override Builder? VisitParameter(ParameterSyntax node)
    {
        Throw.IfNullArgument(node);

        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(node);
        if (semanticModel.GetSymbolInfo(node).Symbol is not IParameterSymbol parameterSymbol)
            return CommonSchemas.UnsupportedObject;

        Metadata metadata = Throw.ForUnexpectedNull(_metadataVisitor.Visit(parameterSymbol));

        TypeSyntax? typeSyntax = node.Type;
        if (typeSyntax is null)
            return CommonSchemas.UnsupportedObject;

        JsonValue? defaultValue = null;
        if (node.Default?.Value is ExpressionSyntax initializer)
        {
            if (semanticModel.GetConstantValue(initializer) is { HasValue: true } constantValue)
            {
                defaultValue = JsonValue.Create(constantValue.Value);
            }
        }

        return GetPropertySchema(typeSyntax, metadata, defaultValue);
    }

    public override Builder? VisitIdentifierName(IdentifierNameSyntax node)
    {
        Throw.IfNullArgument(node);

        using var trace = TraceScope.Enter(node.Identifier.Text);

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
        using var trace = TraceScope.Enter(node.Identifier.Text);

        if (node.IsUnboundGenericName)
            return CommonSchemas.UnsupportedObject;

        SemanticModel semanticModel = _semanticModelCache.GetSemanticModel(node);

        if (semanticModel.GetTypeInfo(node).Type is not ITypeSymbol boundTypeSymbol)
            return CommonSchemas.UnsupportedObject.Comment($"{node.Identifier.Text}: Node does not produce a type symbol.");

        if (!InitializeGenericTypeSymbols(out Builder? errorBuilder))
            return errorBuilder;

        // Check if the symbol is a dictionary and get the bound interface symbol
        INamedTypeSymbol? boundDictionarySymbol = boundTypeSymbol.ImplementsGenericInterface(_dictionaryOfKVSymbol)
            ?? boundTypeSymbol.ImplementsGenericInterface(_readOnlyDictionaryOfKVSymbol);

        if (boundDictionarySymbol is not null)
        {
            ITypeSymbol keyTypeSymbol = boundDictionarySymbol.TypeArguments.First();
            if (keyTypeSymbol.GetSchemaValueType() != SchemaValueType.String)
            {
                return CommonSchemas.UnsupportedObject.Comment($"{node.Identifier.Text}: Key type must be a string.");
            }

            ITypeSymbol valueTypeSymbol = boundDictionarySymbol.TypeArguments.Last();
            if (!valueTypeSymbol.IsJsonValueType(out Builder? valueSchema))
            {
                valueSchema = valueTypeSymbol.FindTypeDeclaration()?.Accept(this);
                if (valueSchema is null)
                    return CommonSchemas.UnsupportedObject.Comment($"{node.Identifier.Text}: Could not find schema for value type.");
            }

            return CommonSchemas.Object.AdditionalProperties(valueSchema);
        }

        // Check if the symbol is an enumerable and get the bound interface symbol
        INamedTypeSymbol? boundEnumerableSymbol = boundTypeSymbol.AllInterfaces
            .FirstOrDefault(i => i.OriginalDefinition.Equals(_enumerableOfTSymbol, SymbolEqualityComparer.Default));

        if (boundEnumerableSymbol is not null)
        {
            ITypeSymbol elementTypeSymbol = boundEnumerableSymbol.TypeArguments.First();
            if (!elementTypeSymbol.IsJsonValueType(out Builder? elementSchema))
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
        using var trace = TraceScope.Enter(node.Kind().ToString());

        return CommonSchemas.Nullable(
            Throw.ForUnexpectedNull(
                node.ElementType.Accept(this)));
    }

    public override Builder? VisitPredefinedType(PredefinedTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = TraceScope.Enter(node.Keyword.Text);

        if (_semanticModelCache.GetSemanticModel(node).GetTypeInfo(node).Type is not INamedTypeSymbol typeSymbol)
        {
            return CommonSchemas.UnsupportedObject;
        }

        if (typeSymbol.IsJsonValueType(out Builder? valueTypeSchema))
        {
            return valueTypeSchema;
        }

        return CommonSchemas.UnsupportedObject;
    }

    public override Builder? VisitArrayType(ArrayTypeSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = TraceScope.Enter(node.Kind().ToString());

        Builder? elementSchema = node.ElementType.Accept(this);
        if (elementSchema is null)
            return CommonSchemas.UnsupportedObject;

        return CommonSchemas.ArrayOf(elementSchema);
    }

    private Builder? VisitTypeDeclaration(TypeDeclarationSyntax node)
    {
        Builder builder = CommonSchemas.Object;

        var properties = new Dictionary<string, JsonSchema>();

        foreach (PropertyDeclarationSyntax property in node.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (property.Accept(this) is Builder propertyBuilder)
            {
                properties[property.Identifier.Text] = propertyBuilder;
            }
        }

        foreach (ParameterSyntax parameter in node.ParameterList?.Parameters ?? [])
        {
            if (parameter.Accept(this) is Builder parameterBuilder)
            {
                properties[parameter.Identifier.Text] = parameterBuilder;
            }
        }

        return builder.Properties(properties);
    }

    private Builder GetPropertySchema(TypeSyntax typeSyntax, Metadata metadata, JsonNode? defaultValue)
    {
        if (typeSyntax.Accept(this) is not Builder typeSchema)
            return CommonSchemas.UnsupportedObject;

        typeSchema.Apply(metadata);

        if (defaultValue is not null)
            typeSchema.Default(defaultValue);

        return typeSchema;
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
