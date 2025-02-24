using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Utilities;
using SharpSchema.Generator.Model;
using Json.Schema;
using Humanizer;
using SharpSchema.Annotations;
using System.Text.Json.Nodes;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;

internal class TypeSchemaSymbolVisitor : SymbolVisitor<BaseTypeDeclarationSyntax, Builder?>
{
    private readonly CSharpSyntaxVisitor<Builder?> _syntaxVisitor;
    private readonly SemanticModelCache _semanticModelCache;
    private readonly GeneratorOptions _options;

    public TypeSchemaSymbolVisitor(
        CSharpSyntaxVisitor<Builder?> visitor,
        SemanticModelCache semanticModelCache,
        GeneratorOptions options)
    {
        _syntaxVisitor = visitor;
        _semanticModelCache = semanticModelCache;
        _options = options;
    }

    protected override Builder? DefaultResult { get; }

    public override Builder? VisitNamedType(INamedTypeSymbol symbol, BaseTypeDeclarationSyntax argument)
    {
        using var scope = Tracer.Enter($"[SYMBOL] {symbol.Name}");

        Dictionary<string, JsonSchema> properties = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> _requiredProperties = new(StringComparer.OrdinalIgnoreCase);

        if (symbol.IsRecord)
        {
            IMethodSymbol primaryCtor = symbol.Constructors.First();
            primaryCtor.Parameters.ForEach(param =>
            {
                Builder? typeBuilder = param.Accept(this, argument);
                if (typeBuilder is null)
                    return;

                string propertyName = param.Name.Camelize();
                properties.Add(propertyName, typeBuilder);

                bool isNullable = param.NullableAnnotation switch
                {
                    NullableAnnotation.NotAnnotated => false,
                    NullableAnnotation.Annotated => true,
                    NullableAnnotation.None => false, // TODO: Make Configurable
                    _ => throw new NotSupportedException()
                };

                bool isRequired = !isNullable;
                if (param.HasExplicitDefaultValue)
                    isRequired = false;

                if (EvaluateSchemaRequired(param, isRequired))
                    _requiredProperties.Add(propertyName);
            });
        }

        symbol.GetMembers().OfType<IPropertySymbol>().ForEach(prop =>
        {
            Builder? valueBuilder = prop.Accept(this, argument);
            if (valueBuilder is null)
                return;

            string propertyName = prop.Name.Camelize();
            properties.Add(propertyName, valueBuilder);

            bool isNullable = prop.NullableAnnotation switch
            {
                NullableAnnotation.NotAnnotated => false,
                NullableAnnotation.Annotated => true,
                NullableAnnotation.None => false, // TODO: Make Configurable
                _ => throw new NotSupportedException()
            };

            bool isRequired = !isNullable;
            if (EvaluateSchemaRequired(prop, isRequired))
                _requiredProperties.Add(propertyName);
        });

        Builder builder = CommonSchemas.Object;
        if (properties.Count > 0)
            builder = builder.Properties(properties);

        if (_requiredProperties.Count > 0)
            builder = builder.Required(_requiredProperties);

        if (symbol.Accept(MemberMeta.SymbolVisitor.Default) is MemberMeta meta)
            builder = builder.ApplyMemberMeta(meta);

        return builder;

        static bool EvaluateSchemaRequired(ISymbol prop, bool isRequired)
        {
            AttributeHandler schemaRequired = prop.GetAttributeHandler<SchemaRequiredAttribute>();
            if (schemaRequired[0] is bool overrideRequired)
                return overrideRequired;

            return isRequired;
        }
    }

    public override Builder? VisitProperty(IPropertySymbol symbol, BaseTypeDeclarationSyntax argument)
    {
        using var scope = Tracer.Enter($"[SYMBOL] {symbol.Name}");

        if (!_options.ShouldProcess(symbol) || !symbol.IsValidForGeneration())
            return null;

        // Excludes generated properties.
        if (symbol.FindDeclaringSyntax() is PropertyDeclarationSyntax pdx)
        {
            Builder? typeBuilder = pdx.Type.Accept(_syntaxVisitor);
            if (typeBuilder is null)
                return null;

            if (pdx.ExpressionBody is ArrowExpressionClauseSyntax aec
                && GetConstantValue(aec.Expression) is JsonNode constantValue)
            {
                typeBuilder = CommonSchemas.Object.Const(constantValue);
            }
            else if (ExtractDefaultValue(pdx) is JsonNode defaultValue)
            {
                typeBuilder = typeBuilder.Default(defaultValue);
            }

            if (symbol.Accept(MemberMeta.SymbolVisitor.Default) is MemberMeta meta)
                typeBuilder = typeBuilder.ApplyMemberMeta(meta);

            return typeBuilder;
        }

        return null;

        // -- Local functions --

        JsonNode? ExtractDefaultValue(PropertyDeclarationSyntax propertyDeclaration)
        {
            return propertyDeclaration.Initializer is EqualsValueClauseSyntax evc
                ? GetConstantValue(evc.Value)
                : null;
        }

        JsonNode? GetConstantValue(SyntaxNode node)
        {
            SemanticModel sm = _semanticModelCache.GetSemanticModel(node);
            return sm.GetConstantValue(node) is Optional<object?> optValue
                && optValue.HasValue
                ? JsonValue.Create(optValue.Value)
                : (JsonNode?)null;
        }
    }

    public override Builder? VisitParameter(IParameterSymbol symbol, BaseTypeDeclarationSyntax argument)
    {
        using var scope = Tracer.Enter($"[SYMBOL] {symbol.Name}");

        if (!_options.ShouldProcess(symbol) || !symbol.IsValidForGeneration())
            return null;

        // Excludes implicitly-typed parameters.
        if (symbol.FindDeclaringSyntax() is ParameterSyntax px && px.Type is TypeSyntax tx)
        {
            Builder? typeBuilder = px.Type.Accept(_syntaxVisitor);
            if (typeBuilder is null)
                return null;

            if (symbol.HasExplicitDefaultValue
                && symbol.ExplicitDefaultValue is object edv
                && JsonValue.Create(edv) is JsonNode defaultValue)
            {
                typeBuilder = typeBuilder.Default(defaultValue);
            }

            if (symbol.Accept(MemberMeta.SymbolVisitor.Default) is MemberMeta meta)
                typeBuilder = typeBuilder.ApplyMemberMeta(meta);

            return typeBuilder;
        }

        return null;
    }
}
