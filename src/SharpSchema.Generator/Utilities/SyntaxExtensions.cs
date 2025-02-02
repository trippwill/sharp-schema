using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

internal static class SyntaxExtensions
{
    public static bool IsNestedInSystemNamespace(this TypeDeclarationSyntax node)
    {
        return node.Ancestors()
            .OfType<NamespaceDeclarationSyntax>()
            .Any(n => n.Name.ToString().StartsWith("System"));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ISymbol? GetDeclaredSymbol(this SyntaxNode node, SemanticModelCache semanticCache)
    {
        return GetDeclaredSymbol(node, semanticCache.GetSemanticModel(node));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ISymbol? GetDeclaredSymbol(this SyntaxNode node, SemanticModel semanticModel)
    {
        return semanticModel.GetDeclaredSymbol(node);
    }
}
