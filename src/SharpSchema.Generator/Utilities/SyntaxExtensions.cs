using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpSchema.Generator.Utilities;

internal static class SyntaxExtensions
{
    public static bool IsNestedInSystemNamespace(this TypeDeclarationSyntax node)
    {
        return node.Ancestors()
            .OfType<NamespaceDeclarationSyntax>()
            .Any(n => n.Name.ToString().StartsWith("System"));
    }
}
