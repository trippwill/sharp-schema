using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

using Builder = JsonSchemaBuilder;

internal class CollectionSymbolVisitor : SymbolVisitor<Builder?>
{
    private const string IEnumerableMetadataName = "System.Collections.Generic.IEnumerable`1";
    private const string IDictionaryMetadataName = "System.Collections.Generic.IDictionary`2";
    private const string IReadOnlyDictionaryMetadataName = "System.Collections.Generic.IReadOnlyDictionary`2";

    private readonly GeneratorOptions _options;
    private readonly CSharpSyntaxVisitor<Builder?> _visitor;
    private readonly INamedTypeSymbol _dictionaryOfKVSymbol;
    private readonly INamedTypeSymbol _readOnlyDictionaryOfKVSymbol;
    private readonly INamedTypeSymbol _enumerableOfTSymbol;

    public CollectionSymbolVisitor(
        GeneratorOptions options,
        Compilation compilation,
        CSharpSyntaxVisitor<Builder?> declaredTypeSyntaxVisitor)
    {
        _enumerableOfTSymbol = compilation.GetTypeByMetadataName(IEnumerableMetadataName)
            ?? throw new InvalidOperationException($"Could not find symbol for '{IEnumerableMetadataName}'.");
        _readOnlyDictionaryOfKVSymbol = compilation.GetTypeByMetadataName(IReadOnlyDictionaryMetadataName)
            ?? throw new InvalidOperationException($"Could not find symbol for '{IReadOnlyDictionaryMetadataName}'.");
        _dictionaryOfKVSymbol = compilation.GetTypeByMetadataName(IDictionaryMetadataName) ??
            throw new InvalidOperationException($"Could not find symbol for '{IDictionaryMetadataName}'.");
        _options = options;
        _visitor = declaredTypeSyntaxVisitor;
    }

    public override Builder? VisitNamedType(INamedTypeSymbol symbol)
    {
        return ResolveCollectionType(symbol);
    }

    private Builder? ResolveCollectionType(INamedTypeSymbol boundTypeSymbol)
    {
        using var trace = Tracer.Enter(boundTypeSymbol.Name);

        // Try dictionary resolution
        if (boundTypeSymbol.ImplementsGenericInterface(_dictionaryOfKVSymbol, _readOnlyDictionaryOfKVSymbol)
            is INamedTypeSymbol boundDictionarySymbol)
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
                        builder.Comment($"Ensure key type '{keyTypeSymbol.Name}' is convertible to string.");
                        break;
                    case DictionaryKeyMode.Strict:
                        return CommonSchemas.UnsupportedObject($"Key type '{keyTypeSymbol.Name}' must be string.");
                    case DictionaryKeyMode.Skip:
                        return new Builder();
                }
            }

            INamedTypeSymbol valueTypeSymbol = (INamedTypeSymbol)boundDictionarySymbol.TypeArguments.Last();
            if (!valueTypeSymbol.IsJsonDefinedType(out Builder? valueSchema))
            {
                valueSchema = _visitor.Visit(valueTypeSymbol.FindTypeDeclaration());
                if (valueSchema is null)
                    return CommonSchemas.UnsupportedObject($"Could not find schema for value type '{valueTypeSymbol.Name}'.");
            }

            return builder.AdditionalProperties(valueSchema);
        }

        // Try enumerable resolution
        if (boundTypeSymbol.ImplementsGenericInterface(_enumerableOfTSymbol)
            is INamedTypeSymbol boundEnumerableSymbol)
        {
            trace.WriteLine("Enumerable type found.");
            INamedTypeSymbol elementTypeSymbol = (INamedTypeSymbol)boundEnumerableSymbol.TypeArguments.First();
            if (!elementTypeSymbol.IsJsonDefinedType(out Builder? elementSchema))
            {
                elementSchema = _visitor.Visit(elementTypeSymbol.FindTypeDeclaration());
                if (elementSchema is null)
                    return CommonSchemas.UnsupportedObject($"Could not find schema for element type '{elementTypeSymbol.Name}'.");
            }

            return CommonSchemas.ArrayOf(elementSchema);
        }

        return null;
    }
}
