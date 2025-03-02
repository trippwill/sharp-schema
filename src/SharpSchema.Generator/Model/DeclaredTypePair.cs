using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpSchema.Generator.Model;

internal readonly record struct DeclaredTypePair(TypeDeclarationSyntax SyntaxNode, INamedTypeSymbol Symbol);
