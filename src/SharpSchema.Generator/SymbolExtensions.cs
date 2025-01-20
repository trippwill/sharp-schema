using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Annotations;

namespace SharpSchema.Generator;

internal static class SymbolExtensions
{
    public static bool ShouldProcessAccessibility(this INamedTypeSymbol symbol, SchemaRootInfoGenerator.Options options)
    {
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => options.TypeOptions.AllowedAccessibilities.CheckFlag(AllowedAccessibilities.Public),
            Accessibility.Internal => options.TypeOptions.AllowedAccessibilities.CheckFlag(AllowedAccessibilities.Internal),
            Accessibility.Private => options.TypeOptions.AllowedAccessibilities.CheckFlag(AllowedAccessibilities.Private),
            _ => false,
        };
    }

    public static bool ShouldProcessAccessibility(this IPropertySymbol symbol, SchemaRootInfoGenerator.Options options)
    {
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => options.MemberOptions.AllowedAccessibilities.CheckFlag(AllowedAccessibilities.Public),
            Accessibility.Internal => options.MemberOptions.AllowedAccessibilities.CheckFlag(AllowedAccessibilities.Internal),
            Accessibility.Private => options.MemberOptions.AllowedAccessibilities.CheckFlag(AllowedAccessibilities.Private),
            _ => false,
        };
    }

    public static AttributeData? GetAttributeData<T>(this ISymbol symbol) where T : Attribute
    {
        return symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == typeof(T).Name);
    }

    public static bool IsValidForGeneration(this IPropertySymbol symbol)
    {
        return !symbol.IsStatic
            && !symbol.IsAbstract
            && !symbol.IsIndexer
            && !symbol.IsVirtual
            && !symbol.IsWriteOnly
            && !symbol.IsImplicitlyDeclared;
    }

    public static bool IsValidForGeneration(this INamedTypeSymbol symbol)
    {
        return !symbol.IsStatic
            && !symbol.IsAbstract
            && !symbol.IsAnonymousType
            && !symbol.IsComImport
            && !symbol.IsImplicitClass
            && !symbol.IsExtern
            && !symbol.IsImplicitlyDeclared;
    }

    public static bool IsIgnoredForGeneration(this ISymbol symbol)
    {
        if (symbol.GetAttributeData<SchemaIgnoreAttribute>() is not null) return true;

        if (symbol.GetAttributeData<JsonIgnoreAttribute>() is AttributeData jsonIgnoreAttribute)
        {
            // Filter out properties that have JsonIgnoreAttribute without any named arguments
            if (jsonIgnoreAttribute.NamedArguments.Length == 0)
                return true;

            // Filter out properties that have JsonIgnoreAttribute with named argument "Condition" and value "Always"
            if (jsonIgnoreAttribute.GetNamedArgument<JsonIgnoreCondition>("Condition") == JsonIgnoreCondition.Always)
                return true;
        }

        return false;
    }

    public static bool TryGetConstructorArgument<TAttribute, TResult>(this ISymbol symbol, int argumentIndex, [NotNullWhen(true)] out TResult? result)
        where TAttribute : Attribute
        where TResult : notnull
    {
        result = default;
        AttributeData? attributeData = symbol.GetAttributeData<TAttribute>();
        if (attributeData is null) return false;

        result = attributeData.GetConstructorArgument<TResult>(argumentIndex);
        return result is not null;
    }

    public static bool IsNestedInSystemNamespace(this TypeDeclarationSyntax node)
    {
        return node.Ancestors()
            .OfType<NamespaceDeclarationSyntax>()
            .Any(n => n.Name.ToString().StartsWith("System"));
    }
}
