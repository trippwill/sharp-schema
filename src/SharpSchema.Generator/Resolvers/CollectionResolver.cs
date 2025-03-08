using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator.Resolvers;

internal enum CollectionKind
{
    None,
    Array,
    Dictionary
}

internal class CollectionResolver
{
    internal readonly record struct Result(CollectionKind Kind, SchemaValueType KeyType, ITypeSymbol ElementSymbol);

    private const string IEnumerableMetadataName = "System.Collections.Generic.IEnumerable`1";
    private const string IDictionaryMetadataName = "System.Collections.Generic.IDictionary`2";
    private const string IReadOnlyDictionaryMetadataName = "System.Collections.Generic.IReadOnlyDictionary`2";

    private readonly INamedTypeSymbol _dictionaryOfKVSymbol;
    private readonly INamedTypeSymbol _readOnlyDictionaryOfKVSymbol;
    private readonly INamedTypeSymbol _enumerableOfTSymbol;

    public CollectionResolver(Compilation compilation)
    {
        _enumerableOfTSymbol = compilation.GetTypeByMetadataName(IEnumerableMetadataName)
            ?? throw new InvalidOperationException($"Could not find symbol for '{IEnumerableMetadataName}'.");

        _readOnlyDictionaryOfKVSymbol = compilation.GetTypeByMetadataName(IReadOnlyDictionaryMetadataName)
            ?? throw new InvalidOperationException($"Could not find symbol for '{IReadOnlyDictionaryMetadataName}'.");

        _dictionaryOfKVSymbol = compilation.GetTypeByMetadataName(IDictionaryMetadataName) ??
            throw new InvalidOperationException($"Could not find symbol for '{IDictionaryMetadataName}'.");
    }

    public Result Resolve(INamedTypeSymbol boundTypeSymbol)
    {
        using var trace = Tracer.Enter(boundTypeSymbol.Name);

        // Try dictionary resolution
        if (boundTypeSymbol.ImplementsGenericInterface(_dictionaryOfKVSymbol, _readOnlyDictionaryOfKVSymbol)
            is INamedTypeSymbol boundDictionarySymbol)
        {
            trace.WriteLine("Dictionary type found.");

            ITypeSymbol keyTypeSymbol = boundDictionarySymbol.TypeArguments.First();
            ITypeSymbol valueTypeSymbol = boundDictionarySymbol.TypeArguments.Last();

            return new(
                CollectionKind.Dictionary,
                keyTypeSymbol.GetSchemaValueType(),
                valueTypeSymbol);
        }

        // Try enumerable resolution
        if (boundTypeSymbol.ImplementsGenericInterface(_enumerableOfTSymbol) is INamedTypeSymbol boundEnumerableSymbol)
        {
            trace.WriteLine("Enumerable type found.");
            ITypeSymbol elementTypeSymbol = boundEnumerableSymbol.TypeArguments.First();
            return new(
                CollectionKind.Array,
                SchemaValueType.Null,
                elementTypeSymbol);
        }

        trace.WriteLine("Not a recognized collection.");
        return new(CollectionKind.None, SchemaValueType.Null, null!);
    }
}
