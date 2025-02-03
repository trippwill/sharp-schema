using System.Runtime.CompilerServices;
using Humanizer;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Generator.Model;
using SharpSchema.Annotations;

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
        var requiredProperties = new List<string>();

        foreach (MemberDeclarationSyntax member in node.Members)
        {
            ProcessMember(member);
        }

        // Collect primary-constructor parameters
        if (node.ParameterList is not null)
        {
            foreach (ParameterSyntax parameter in node.ParameterList.Parameters)
            {
                ProcessParameter(parameter);
            }
        }

        // Apply collected properties and required properties
        if (properties.Count > 0)
        {
            builder = builder.Properties(properties);
        }

        if (requiredProperties.Count > 0)
        {
            builder = builder.Required(requiredProperties);
        }

        return builder;

        void ProcessMember(MemberDeclarationSyntax member)
        {
            if (member is PropertyDeclarationSyntax property && typeVisitor.Visit(property) is Builder propertyBuilder)
            {
                properties[property.Identifier.Text.Camelize()] = propertyBuilder;

                // Check for nullability annotation
                bool isNullable = property.Type is NullableTypeSyntax;

                // Check for SchemaRequired attribute
                bool hasSchemaRequiredAttribute = property.AttributeLists
                    .SelectMany(attrList => attrList.Attributes)
                    .Any(attr => attr.Name.ToString() == nameof(SchemaRequiredAttribute));

                // Check for required keyword
                bool hasRequiredKeyword = property.Modifiers.Any(SyntaxKind.RequiredKeyword);

                if (!isNullable || hasSchemaRequiredAttribute || hasRequiredKeyword)
                {
                    requiredProperties.Add(property.Identifier.Text.Camelize());
                }
            }
        }

        void ProcessParameter(ParameterSyntax parameter)
        {
            if (typeVisitor.Visit(parameter) is Builder paramBuilder)
            {
                properties[parameter.Identifier.Text.Camelize()] = paramBuilder;

                // Check for default value
                bool hasDefaultValue = parameter.Default is not null;

                // Check for nullability annotation
                bool isNullable = parameter.Type is NullableTypeSyntax;

                // Check for SchemaRequired attribute
                bool hasSchemaRequiredAttribute = parameter.AttributeLists
                    .SelectMany(attrList => attrList.Attributes)
                    .Any(attr => attr.Name.ToString() == nameof(SchemaRequiredAttribute));

                if (!hasDefaultValue && !isNullable && !hasSchemaRequiredAttribute)
                {
                    requiredProperties.Add(parameter.Identifier.Text.Camelize());
                }
            }
        }
    }
}
