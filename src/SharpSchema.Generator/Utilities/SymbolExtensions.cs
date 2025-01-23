using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Annotations;

namespace SharpSchema.Generator.Utilities;

/// <summary>
/// Provides extension methods for symbols.
/// </summary>
internal static class SymbolExtensions
{
    /// <summary>
    /// Determines if the symbol should be processed based on its accessibility and the provided options.
    /// </summary>
    /// <param name="symbol">The named type symbol.</param>
    /// <param name="options">The schema tree generator options.</param>
    /// <returns>True if the symbol should process accessibility; otherwise, false.</returns>
    public static bool ShouldProcessAccessibility(this INamedTypeSymbol symbol, SchemaTreeGenerator.Options options)
    {
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => options.TypeOptions.CheckFlag(AllowedAccessibilities.Public),
            Accessibility.Internal => options.TypeOptions.CheckFlag(AllowedAccessibilities.Internal),
            Accessibility.Private => options.TypeOptions.CheckFlag(AllowedAccessibilities.Private),
            _ => false,
        };
    }

    /// <summary>
    /// Determines if the property symbol should be processed based on its accessibility and the provided options.
    /// </summary>
    /// <param name="symbol">The property symbol.</param>
    /// <param name="options">The schema tree generator options.</param>
    /// <returns>True if the property symbol should process accessibility; otherwise, false.</returns>
    public static bool ShouldProcessAccessibility(this IPropertySymbol symbol, SchemaTreeGenerator.Options options)
    {
        return symbol.DeclaredAccessibility switch
        {
            Accessibility.Public => options.MemberOptions.CheckFlag(AllowedAccessibilities.Public),
            Accessibility.Internal => options.MemberOptions.CheckFlag(AllowedAccessibilities.Internal),
            Accessibility.Private => options.MemberOptions.CheckFlag(AllowedAccessibilities.Private),
            _ => false,
        };
    }

    /// <summary>
    /// Gets the attribute data of the specified type from the symbol.
    /// </summary>
    /// <typeparam name="T">The type of the attribute.</typeparam>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The attribute data if found; otherwise, null.</returns>
    public static AttributeData? GetAttributeData<T>(this ISymbol symbol) where T : Attribute
    {
        foreach (var attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.Name == typeof(T).Name)
                return attribute;
        }

        return null;
    }

    /// <summary>
    /// Determines if the property symbol is valid for generation.
    /// </summary>
    /// <param name="symbol">The property symbol.</param>
    /// <returns>True if the property symbol is valid for generation; otherwise, false.</returns>
    public static bool IsValidForGeneration(this IPropertySymbol symbol)
    {
        return !symbol.IsStatic
            && !symbol.IsAbstract
            && !symbol.IsIndexer
            && !symbol.IsVirtual
            && !symbol.IsWriteOnly
            && !symbol.IsImplicitlyDeclared;
    }

    /// <summary>
    /// Determines if the named type symbol is valid for generation.
    /// </summary>
    /// <param name="symbol">The named type symbol.</param>
    /// <returns>True if the named type symbol is valid for generation; otherwise, false.</returns>
    public static bool IsValidForGeneration(this INamedTypeSymbol symbol)
    {
        return !symbol.IsStatic
            && !symbol.IsAnonymousType
            && !symbol.IsComImport
            && !symbol.IsImplicitClass
            && !symbol.IsExtern
            && !symbol.IsImplicitlyDeclared;
    }

    /// <summary>
    /// Determines if the symbol is ignored for generation based on specific attributes.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>True if the symbol is ignored for generation; otherwise, false.</returns>
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

    /// <summary>
    /// Finds the type declaration syntax for the symbol.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The type declaration syntax if found; otherwise, null.</returns>
    public static TypeDeclarationSyntax? FindTypeDeclaration(this ISymbol symbol)
    {
        return symbol.DeclaringSyntaxReferences
            .Select(r => r.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .FirstOrDefault();
    }

    /// <summary>
    /// Checks if the symbol implements the specified generic interface.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <param name="interfaceSymbol">The generic interface symbol to check against.</param>
    /// <returns>The implemented generic interface symbol if found; otherwise, null.</returns>
    public static INamedTypeSymbol? ImplementsGenericInterface(this ITypeSymbol symbol, INamedTypeSymbol interfaceSymbol)
    {
        foreach (var i in symbol.AllInterfaces)
        {
            if (i.OriginalDefinition.Equals(interfaceSymbol, SymbolEqualityComparer.Default))
                return i;
        }

        return null;
    }

    /// <summary>
    /// Checks if the type symbol implements the specified abstract class.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to check.</param>
    /// <param name="abstractClassSymbol">The abstract class symbol to check against.</param>
    /// <returns>True if the type symbol implements the abstract class; otherwise, false.</returns>
    public static bool ImplementsAbstractClass(this ITypeSymbol typeSymbol, INamedTypeSymbol abstractClassSymbol)
    {
        INamedTypeSymbol? baseType = typeSymbol.BaseType;
        while (baseType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseType, abstractClassSymbol))
                return true;

            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Tries to get the constructor argument of the specified attribute type from the symbol.
    /// </summary>
    /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="symbol">The symbol.</param>
    /// <param name="argumentIndex">The index of the argument.</param>
    /// <param name="result">The result if found; otherwise, default.</param>
    /// <returns>True if the constructor argument is found; otherwise, false.</returns>
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

    public static long GetSchemaHash(this ISymbol symbol) => SchemaHash.Combine(
        symbol.Name.GetSchemaHash(),
        symbol.ContainingNamespace.Name.GetSchemaHash(),
        symbol.ContainingAssembly.Name.GetSchemaHash(),
        (long)symbol.Kind);

    /// <summary>
    /// Gets the schema value type for the symbol.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The schema value type.</returns>
    public static SchemaValueType GetSchemaValueType(this ISymbol symbol)
    {
        return symbol.Name switch
        {
            "Boolean" => SchemaValueType.Boolean,
            "Byte" => SchemaValueType.Integer,
            "Char" => SchemaValueType.String,
            "DateTime" => SchemaValueType.String,
            "Decimal" => SchemaValueType.Number,
            "Double" => SchemaValueType.Number,
            "Int16" => SchemaValueType.Integer,
            "Int32" => SchemaValueType.Integer,
            "Int64" => SchemaValueType.Integer,
            "Object" => SchemaValueType.Object,
            "SByte" => SchemaValueType.Integer,
            "Single" => SchemaValueType.Number,
            "String" => SchemaValueType.String,
            "UInt16" => SchemaValueType.Integer,
            "UInt32" => SchemaValueType.Integer,
            "UInt64" => SchemaValueType.Integer,
            _ => SchemaValueType.Object
        };
    }
}
