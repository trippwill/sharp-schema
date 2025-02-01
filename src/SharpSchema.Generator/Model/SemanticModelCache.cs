using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator.Model;

internal class SemanticModelCache 
{
    private readonly Compilation _compilation;
    private readonly Dictionary<SyntaxTree, SemanticModel> _cache;

    public SemanticModelCache(Compilation compilation)
    {
        _compilation = compilation;
        _cache = [];
    }

    public SemanticModel GetSemanticModel(SyntaxTree syntaxTree)
    {
        if (_cache.TryGetValue(syntaxTree, out var model))
        {
            using var trace = TraceScope.Enter("[HIT]");
            return model;
        }

        using var missTrace = TraceScope.Enter("[MISS]");
        model = _compilation.GetSemanticModel(syntaxTree);
        return _cache[syntaxTree] = model;
    }

    public SemanticModel GetSemanticModel(SyntaxNode node) => GetSemanticModel(node.SyntaxTree);
}
