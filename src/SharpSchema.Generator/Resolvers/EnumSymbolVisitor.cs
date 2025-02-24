using Humanizer;
using Json.Schema;
using Microsoft.CodeAnalysis;
using SharpSchema.Annotations;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator.Resolvers;

using Builder = JsonSchemaBuilder;

internal class EnumSymbolVisitor : SymbolVisitor<GeneratorOptions, Builder?>
{
    public static EnumSymbolVisitor Instance { get; } = new EnumSymbolVisitor();

    private EnumSymbolVisitor() { }

    protected override Builder? DefaultResult => null;

    public override Builder? VisitNamedType(INamedTypeSymbol symbol, GeneratorOptions options)
    {
        using var trace = Tracer.Enter(symbol.Name);

        if (symbol.TypeKind != TypeKind.Enum)
            return DefaultResult;

        trace.WriteLine($"{options.EnumMode}");

        if (options.EnumMode == EnumMode.String)
        {
            var names = symbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Select(fieldSymbol => fieldSymbol.GetAttributeHandler<SchemaEnumValueAttribute>()[0] as string
                    ?? fieldSymbol.Name.Camelize())
                .ToList();

            return CommonSchemas.String.Enum(names);
        }
        else if (options.EnumMode == EnumMode.UnderlyingType)
        {
            trace.WriteLine("Underlying type enum handling.");

            if (symbol.EnumUnderlyingType is INamedTypeSymbol underlyingSymbol
                && underlyingSymbol.IsJsonDefinedType(out Builder? underlyingBuilder))
            {
                return underlyingBuilder;
            }
        }

        return DefaultResult;
    }
}
