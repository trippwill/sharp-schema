using System.Runtime.CompilerServices;
using Humanizer;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

using Builder = JsonSchemaBuilder;

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

    public static Builder CreateTypeSchema(this TypeDeclarationSyntax node, CSharpSyntaxVisitor<Builder?> typeVisitor)
    {
        Throw.IfNullArgument(node);

        Builder builder = CommonSchemas.Object;

        var properties = new Dictionary<string, JsonSchema>();

        foreach (MemberDeclarationSyntax member in node.Members)
        {
            if (member is PropertyDeclarationSyntax property && typeVisitor.Visit(property) is Builder propertyBuilder)
            {
                properties[property.Identifier.Text.Camelize()] = propertyBuilder;
            }
        }

        // Collect primary-constructor parameters
        if (node.ParameterList is not null)
        {
            foreach (ParameterSyntax parameter in node.ParameterList.Parameters)
            {
                if (typeVisitor.Visit(parameter) is Builder paramBuilder)
                {
                    properties[parameter.Identifier.Text.Camelize()] = paramBuilder;
                }
            }
        }

        // Apply collected properties
        return builder.Properties(properties);
    }
}
