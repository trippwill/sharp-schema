using System.Diagnostics.CodeAnalysis;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

/// <summary>
/// Provides extension methods for symbols.
/// </summary>
internal static class SymbolExtensions
{
    public static AttributeHandler GetAttributeHandler<T>(this ISymbol symbol, Traversal traversal = Traversal.SymbolOnly)
        where T : Attribute
    {
        return new AttributeHandler(GetAttributeData<T>(symbol, traversal));
    }

    /// <summary>
    /// Gets the attribute data of the specified type from the symbol.
    /// </summary>
    /// <typeparam name="T">The type of the attribute.</typeparam>
    /// <param name="symbol">The symbol.</param>
    /// <param name="traversal">Indicates whether to search for attributes on base classes and interfaces.</param>
    /// <returns>The attribute data if found; otherwise, null.</returns>
    public static AttributeData? GetAttributeData<T>(this ISymbol symbol, Traversal traversal = Traversal.SymbolOnly)
        where T : Attribute
    {
        // Search for the attribute on the symbol itself
        foreach (AttributeData attribute in symbol.GetAttributes())
        {
            if (attribute.AttributeClass?.MatchesType<T>() ?? false)
                return attribute;
        }

        if (symbol is INamedTypeSymbol namedTypeSymbol)
        {
            if (traversal.CheckFlag(Traversal.Bases))
            {
                // Search on base classes
                INamedTypeSymbol? baseType = namedTypeSymbol.BaseType;
                while (baseType is not null)
                {
                    foreach (AttributeData attribute in baseType.GetAttributes())
                    {
                        if (attribute.AttributeClass?.MatchesType<T>() ?? false)
                            return attribute;
                    }

                    baseType = baseType.BaseType;
                }
            }

            if (traversal.CheckFlag(Traversal.Interfaces))
            {
                // Search on interfaces
                foreach (INamedTypeSymbol interfaceType in namedTypeSymbol.AllInterfaces)
                {
                    foreach (AttributeData attribute in interfaceType.GetAttributes())
                    {
                        if (attribute.AttributeClass?.MatchesType<T>() ?? false)
                            return attribute;
                    }
                }
            }
        }

        return null;
    }

    public static TValue? GetAttributeConstructorArgument<TAttribute, TValue>(this ISymbol symbol, int argumentIndex, Traversal traversal = Traversal.SymbolOnly)
        where TAttribute : Attribute
        where TValue : notnull
    {
        AttributeData? attributeData = symbol.GetAttributeData<TAttribute>(traversal);
        if (attributeData is null)
            return default;

        return attributeData.GetConstructorArgument<TValue>(argumentIndex);
    }

    public static bool MatchesType<T>(this INamedTypeSymbol typeSymbol)
    {
        const string globalPrefix = "global::";

        // Get full metadata name, including generics
        string typeSymbolFullName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        ReadOnlySpan<char> normalizedSymbolName = typeSymbolFullName.AsSpan();
        if (normalizedSymbolName.StartsWith(globalPrefix.AsSpan()))
        {
            normalizedSymbolName = normalizedSymbolName[globalPrefix.Length..];
        }

        // Handle open generic types
        Type runtimeType = typeof(T);
        if (runtimeType.IsGenericTypeDefinition)
        {
            // Convert `typeof(List<>)` to "System.Collections.Generic.List`1"
            string openGenericName = runtimeType.FullName!.Split('[')[0];
            return normalizedSymbolName.StartsWith(openGenericName.AsSpan());
        }

        // Handle concrete generic types
        string runtimeTypeName = runtimeType.FullName!;
        return normalizedSymbolName.SequenceEqual(runtimeTypeName.AsSpan());
    }

    /// <summary>
    /// Determines if the symbol is valid for generation.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>True if the symbol is valid for generation; otherwise, false.</returns>
    public static bool IsValidForGeneration(this ISymbol symbol)
    {
        return !symbol.IsStatic
            && !symbol.IsVirtual
            && !symbol.IsImplicitlyDeclared
            && symbol switch
            {
                IPropertySymbol property => IsValidPropertySymbol(property),
                INamedTypeSymbol namedType => IsValidNamedTypeSymbol(namedType),
                _ => true
            };

        static bool IsValidNamedTypeSymbol(INamedTypeSymbol symbol)
        {
            return !symbol.IsStatic
                && !symbol.IsAnonymousType
                && !symbol.IsComImport
                && !symbol.IsImplicitClass
                && !symbol.IsExtern
                && !symbol.IsImplicitlyDeclared;
        }

        static bool IsValidPropertySymbol(IPropertySymbol symbol)
        {
            return !symbol.IsIndexer
                && !symbol.IsWriteOnly;
        }
    }

    /// <summary>
    /// Finds the type declaration syntax for the symbol.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The type declaration syntax if found; otherwise, null.</returns>
    public static TypeDeclarationSyntax? FindTypeDeclaration(this INamedTypeSymbol symbol)
    {
        foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = reference.GetSyntax();
            if (syntax is TypeDeclarationSyntax typeDeclarationSyntax)
            {
                return typeDeclarationSyntax;
            }
        }

        return null;
    }

    public static BaseTypeDeclarationSyntax? FindBaseTypeDeclaration(this ITypeSymbol symbol)
    {
        foreach (SyntaxReference reference in symbol.DeclaringSyntaxReferences)
        {
            SyntaxNode syntax = reference.GetSyntax();
            if (syntax is BaseTypeDeclarationSyntax baseTypeDeclarationSyntax)
            {
                return baseTypeDeclarationSyntax;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the symbol implements any of the specified generic interface.
    /// </summary>
    /// <param name="symbol">The type symbol to check.</param>
    /// <param name="interfaceSymbols">The generic interface symbols to check against.</param>
    /// <returns>The first implemented generic interface symbol if found; otherwise, null.</returns>
    public static INamedTypeSymbol? ImplementsGenericInterface(this INamedTypeSymbol symbol, params INamedTypeSymbol[] interfaceSymbols)
    {
        foreach (INamedTypeSymbol testInterfaceSymbol in interfaceSymbols)
        {
            if (symbol.TypeKind == TypeKind.Interface
                && symbol.OriginalDefinition.MetadataName == testInterfaceSymbol.OriginalDefinition.MetadataName)
                return symbol;

            return symbol.AllInterfaces.FirstOrDefault(
                i => i.OriginalDefinition.MetadataName == testInterfaceSymbol.OriginalDefinition.MetadataName);
        }

        return null;
    }

    public static bool InheritsFrom(this INamedTypeSymbol symbol, INamedTypeSymbol baseType)
    {
        INamedTypeSymbol? current = symbol.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;
            current = current.BaseType;
        }
        return false;
    }

    public static string GetDefCacheKey(this ITypeSymbol symbol) => symbol.GetDocumentationCommentId() ?? symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static bool IsJsonDefinedType(this ITypeSymbol symbol, [NotNullWhen(true)] out JsonSchemaBuilder? schema)
    {
        if (symbol.SpecialType == SpecialType.None)
        {
            schema = null;
            return false;
        }

        schema = symbol.SpecialType switch
        {
            SpecialType.System_Boolean => CommonSchemas.Boolean,
            SpecialType.System_Byte => CommonSchemas.System_Byte,
            SpecialType.System_Char => CommonSchemas.System_Char,
            SpecialType.System_DateTime => CommonSchemas.System_DateTime,
            SpecialType.System_Decimal => CommonSchemas.System_Decimal,
            SpecialType.System_Double => CommonSchemas.System_Double,
            SpecialType.System_Int16 => CommonSchemas.System_Int16,
            SpecialType.System_Int32 => CommonSchemas.System_Int32,
            SpecialType.System_Int64 => CommonSchemas.System_Int64,
            SpecialType.System_Object => throw new InvalidOperationException("System.Object does not map to a json defined type."),
            SpecialType.System_SByte => CommonSchemas.System_SByte,
            SpecialType.System_Single => CommonSchemas.System_Single,
            SpecialType.System_String => CommonSchemas.String,
            SpecialType.System_UInt16 => CommonSchemas.System_UInt16,
            SpecialType.System_UInt32 => CommonSchemas.System_UInt32,
            SpecialType.System_UInt64 => CommonSchemas.System_UInt64,
            _ => null
        };

        return schema is not null;
    }

    /// <summary>
    /// Gets the schema value type for the symbol.
    /// </summary>
    /// <param name="symbol">The symbol.</param>
    /// <returns>The schema value type.</returns>
    [ExcludeFromCodeCoverage]
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
