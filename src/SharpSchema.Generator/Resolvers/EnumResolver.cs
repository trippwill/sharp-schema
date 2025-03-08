using Humanizer;
using Json.Schema;
using Microsoft.CodeAnalysis;
using SharpSchema.Annotations;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator.Resolvers;

using Builder = JsonSchemaBuilder;

internal class EnumResolver
{
    public static Builder? Resolve(INamedTypeSymbol symbol, GeneratorOptions options)
    {
        using var trace = Tracer.Enter(symbol.Name);

        if (symbol.TypeKind != TypeKind.Enum)
            return null;

        trace.WriteLine($"{options.EnumMode}");

        if (options.EnumMode == EnumMode.String)
        {
            var names = symbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Select(fieldSymbol => fieldSymbol.GetAttributeHandler<SchemaEnumValueAttribute>(TraversalMode.SymbolOnly)[0] as string
                    ?? fieldSymbol.Name.Camelize())
                .ToList();

            return CommonSchemas.String.Enum(names);
        }
        else if (options.EnumMode == EnumMode.UnderlyingType)
        {
            trace.WriteLine("Underlying type enum handling.");

            if (symbol.EnumUnderlyingType is INamedTypeSymbol underlyingSymbol
                && underlyingSymbol.IsJsonDefinedType(NumberMode.JsonNative, out Builder? underlyingBuilder))
            {
                return underlyingBuilder;
            }
        }

        return null;
    }
}
