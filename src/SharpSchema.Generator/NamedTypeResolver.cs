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
using PropertyResult = (JsonSchemaBuilder? Builder, bool Required);

internal class NamedTypeResolver
{
    private readonly RootContext _context;

    public NamedTypeResolver(
        RootContext rootContext)
    {
        _context = rootContext;
    }

    public Builder? Resolve(INamedTypeSymbol symbol, GeneratorOptions options)
    {
        using var scope = Tracer.Enter($"[SYMBOL] {symbol.Name}");

        ObjectAttributes attributes = symbol.GetObjectAttributes(options.TraversalMode);
        options = options.Override(attributes);

        scope.WriteLine(options.ToString(), "Options");

        Dictionary<string, JsonSchema> properties = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> _requiredProperties = new(StringComparer.OrdinalIgnoreCase);

        if (symbol.IsRecord)
        {
            IMethodSymbol primaryCtor = symbol.Constructors.First();
            primaryCtor.Parameters.ForEach(param =>
            {
                string propertyName = param.Name.Camelize();

                (Builder? typeBuilder, bool isRequired) = this.VisitParameter(param, options);
                if (typeBuilder is null)
                    return;

                properties.Add(propertyName, typeBuilder);

                if (isRequired)
                    _requiredProperties.Add(propertyName);
            });
        }

        symbol.GetMembers().OfType<IPropertySymbol>().ForEach(prop =>
        {
            string propertyName = prop.Name.Camelize();

            (Builder? valueBuilder, bool isRequired) = this.VisitProperty(prop, options);
            if (valueBuilder is null)
                return;

            properties.Add(propertyName, valueBuilder);

            if (isRequired)
                _requiredProperties.Add(propertyName);
        });

        Builder builder = CommonSchemas.Object;
        if (symbol.Accept(MemberMeta.SymbolVisitor.Default) is MemberMeta meta)
            builder = builder.ApplyMemberMeta(meta);

        if (properties.Count > 0)
            builder = builder.Properties(properties);

        if (_requiredProperties.Count > 0)
            builder = builder.Required(_requiredProperties);

        if (options.TraversalMode.CheckFlag(TraversalMode.Bases))
        {
            INamedTypeSymbol? @base = symbol.BaseType;
            while (@base is not null && @base.SpecialType is not SpecialType.System_Object)
            {
                NamedTypeResolver baseSymbolVisitor = new(_context);
                if (baseSymbolVisitor.Resolve(@base, options) is Builder baseBuilder)
                {
                    builder = builder.MergeProperties(baseBuilder);
                }

                @base = @base.BaseType;
            }
        }

        if (options.TraversalMode.CheckFlag(TraversalMode.Interfaces))
        {
            symbol.AllInterfaces.ForEach(@interface =>
            {
                NamedTypeResolver interfaceSymbolVisitor = new(_context);
                if (interfaceSymbolVisitor.Resolve(@interface, options) is Builder interfaceBuilder)
                {
                    builder = builder.MergeProperties(interfaceBuilder);
                }
            });
        }

        return builder;
    }

    private PropertyResult VisitProperty(IPropertySymbol symbol, GeneratorOptions options)
    {
        using var scope = Tracer.Enter($"[SYMBOL] {symbol.Name}");

        PropertyAttributes attributes = symbol.GetPropertyAttributes(options.TraversalMode);
        options = options.Override(attributes);

        scope.WriteLine(options.ToString(), "Options");

        if (!options.ShouldProcess(symbol) || !symbol.IsValidForGeneration())
            return (null, false);

        bool isRequired = symbol.IsRequired || !IsNullable(symbol.NullableAnnotation);

        if (symbol.GetOverrideSchema() is Builder overrideBuilder)
            return (overrideBuilder, EvaluateSchemaRequired(symbol, isRequired));

        if (symbol.FindDeclaringSyntax() is not PropertyDeclarationSyntax pdx)
            return (null, false);

        if (pdx.Accept(_context.LeafSyntaxVisitor(options)) is not Builder typeBuilder)
            return (null, false);

        if (typeBuilder.Get<ConstKeyword>() is not null)
        {
            isRequired = true;
        }
        else if (typeBuilder.Get<DefaultKeyword>() is not null)
        {
            isRequired = false;
        }

        if (symbol.Accept(MemberMeta.SymbolVisitor.Default) is MemberMeta meta)
            typeBuilder = typeBuilder.ApplyMemberMeta(meta);

        return (typeBuilder, EvaluateSchemaRequired(symbol, isRequired));
    }

    private PropertyResult VisitParameter(IParameterSymbol symbol, GeneratorOptions options)
    {
        using var scope = Tracer.Enter($"[SYMBOL] {symbol.Name}");

        PropertyAttributes attributes = symbol.GetPropertyAttributes(options.TraversalMode);
        options = options.Override(attributes);

        scope.WriteLine(options.ToString(), "Options");

        if (!options.ShouldProcess(symbol) || !symbol.IsValidForGeneration())
            return (null, false);

        bool isRequired = !IsNullable(symbol.NullableAnnotation);

        if (symbol.GetOverrideSchema() is Builder overrideBuilder)
            return (overrideBuilder, EvaluateSchemaRequired(symbol, isRequired));

        // Excludes implicitly-typed parameters.
        if (symbol.FindDeclaringSyntax() is not ParameterSyntax px || px.Type is null)
            return (null, false);

        if (px.Type.Accept(_context.LeafSyntaxVisitor(options)) is not Builder typeBuilder)
            return (null, false);

        if (symbol.HasExplicitDefaultValue
            && symbol.ExplicitDefaultValue is object edv
            && JsonValue.Create(edv) is JsonNode defaultValue)
        {
            typeBuilder = typeBuilder.Default(defaultValue);
            isRequired = false;
        }

        if (symbol.Accept(MemberMeta.SymbolVisitor.Default) is MemberMeta meta)
            typeBuilder = typeBuilder.ApplyMemberMeta(meta);

        return (typeBuilder, EvaluateSchemaRequired(symbol, isRequired));
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
