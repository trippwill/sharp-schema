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

internal class NamedTypeSymbolVisitor : SymbolVisitor<NamedTypeSymbolVisitor.StateContainer?, Builder?>
{
    public class StateContainer
    {
        internal bool? WasLastPropertyRequired { get; set; }
    }

    private readonly CSharpSyntaxVisitor<Builder?> _syntaxVisitor;
    private readonly SemanticModelCache _semanticModelCache;
    private readonly GeneratorOptions _options;

    public NamedTypeSymbolVisitor(
        CSharpSyntaxVisitor<Builder?> visitor,
        SemanticModelCache semanticModelCache,
        GeneratorOptions options)
    {
        _syntaxVisitor = visitor;
        _semanticModelCache = semanticModelCache;
        _options = options;
    }

    protected override Builder? DefaultResult { get; }

    public override Builder? VisitNamedType(INamedTypeSymbol symbol, StateContainer? state)
    {
        state ??= new StateContainer();
        using var scope = Tracer.Enter($"[SYMBOL] {symbol.Name}");

        Dictionary<string, JsonSchema> properties = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> _requiredProperties = new(StringComparer.OrdinalIgnoreCase);

        if (symbol.IsRecord)
        {
            IMethodSymbol primaryCtor = symbol.Constructors.First();
            primaryCtor.Parameters.ForEach(param =>
            {
                Builder? typeBuilder = param.Accept(this, state);
                if (typeBuilder is null)
                    return;

                string propertyName = param.Name.Camelize();
                properties.Add(propertyName, typeBuilder);

                if (state.WasLastPropertyRequired == true)
                    _requiredProperties.Add(propertyName);

                state.WasLastPropertyRequired = null;
            });
        }

        symbol.GetMembers().OfType<IPropertySymbol>().ForEach(prop =>
        {
            Builder? valueBuilder = prop.Accept(this, state);
            if (valueBuilder is null)
                return;

            string propertyName = prop.Name.Camelize();
            properties.Add(propertyName, valueBuilder);

            if (state.WasLastPropertyRequired == true)
                _requiredProperties.Add(propertyName);

            state.WasLastPropertyRequired = null;
        });

        Builder builder = CommonSchemas.Object;

        if (symbol.Accept(MemberMeta.SymbolVisitor.Default) is MemberMeta meta)
            builder = builder.ApplyMemberMeta(meta);

        if (properties.Count > 0)
            builder = builder.Properties(properties);

        if (_requiredProperties.Count > 0)
            builder = builder.Required(_requiredProperties);

        return builder;
    }

    public override Builder? VisitProperty(IPropertySymbol symbol, StateContainer? state)
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

            bool isRequired = !IsNullable(symbol.NullableAnnotation);

            if (pdx.ExpressionBody is ArrowExpressionClauseSyntax aec
                && GetConstantValue(aec.Expression) is JsonNode constantValue)
            {
                typeBuilder = CommonSchemas.Const(constantValue);
                isRequired = true;
            }
            else if (ExtractDefaultValue(pdx) is JsonNode defaultValue)
            {
                typeBuilder = typeBuilder.Default(defaultValue);
                isRequired = false;
            }

            if (symbol.Accept(MemberMeta.SymbolVisitor.Default) is MemberMeta meta)
                typeBuilder = typeBuilder.ApplyMemberMeta(meta);


            state!.WasLastPropertyRequired = EvaluateSchemaRequired(symbol, isRequired);

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

    public override Builder? VisitParameter(IParameterSymbol symbol, StateContainer? state)
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

            bool isRequired = !IsNullable(symbol.NullableAnnotation); ;

            if (symbol.HasExplicitDefaultValue
                && symbol.ExplicitDefaultValue is object edv
                && JsonValue.Create(edv) is JsonNode defaultValue)
            {
                typeBuilder = typeBuilder.Default(defaultValue);
                isRequired = false;
            }

            if (symbol.Accept(MemberMeta.SymbolVisitor.Default) is MemberMeta meta)
                typeBuilder = typeBuilder.ApplyMemberMeta(meta);

            state!.WasLastPropertyRequired = EvaluateSchemaRequired(symbol, isRequired);

            return typeBuilder;
        }

        return null;
    }

    private static bool IsNullable(NullableAnnotation annotation)
    {
        return annotation switch
        {
            NullableAnnotation.NotAnnotated => false,
            NullableAnnotation.Annotated => true,
            NullableAnnotation.None => false, // TODO: Make Configurable
            _ => throw new NotSupportedException()
        };
    }

    private static bool EvaluateSchemaRequired(ISymbol prop, bool isRequired)
    {
        AttributeHandler schemaRequired = prop.GetAttributeHandler<SchemaRequiredAttribute>();
        if (schemaRequired[0] is bool overrideRequired)
            return overrideRequired;

        return isRequired;
    }
}
