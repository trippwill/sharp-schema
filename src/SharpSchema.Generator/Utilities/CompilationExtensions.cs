using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

internal static class CompilationExtensions
{
    internal static IEnumerable<DeclaredTypePair> GetAllNamedTypes(this Compilation compilation, SemanticModelCache semanticModelCache)
    {
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel semanticModel = semanticModelCache.GetSemanticModel(tree);
            foreach (TypeDeclarationSyntax type in tree
                .GetRoot()
                .DescendantNodes()
                .OfType<TypeDeclarationSyntax>())
            {
                ISymbol? symbol = type.GetDeclaredSymbol(semanticModel);
                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    yield return new(type, namedTypeSymbol);
                }
            }
        }
    }
}
