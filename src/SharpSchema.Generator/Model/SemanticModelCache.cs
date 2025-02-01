using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator.Model;

internal class SemanticModelCache(Compilation compilation)
{
    private readonly Dictionary<SyntaxTree, SemanticModel> _cache = [];

    public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
    {
        if (_cache.TryGetValue(syntaxTree, out var model))
        {
            using var trace = Tracer.Enter("[HIT]");
            return model;
        }

        using var missTrace = Tracer.Enter("[MISS]");
        model = compilation.GetSemanticModel(syntaxTree);
        return _cache[syntaxTree] = model;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SemanticModel GetSemanticModel(SyntaxNode node) => GetSemanticModel(node.SyntaxTree);
}
