using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using SharpSchema.Annotations;

namespace SharpSchema.Generator;

internal static class SymbolExtensions
{
    public static AttributeData? GetAttributeData<T>(this ISymbol symbol) where T : Attribute
    {
        return symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.Name == typeof(T).Name);
    }

    public static bool IsValidForGeneration(this IPropertySymbol symbol)
    {
        if (symbol.DeclaredAccessibility
            is Accessibility.Public
            or Accessibility.Protected
            or Accessibility.ProtectedOrInternal
            or Accessibility.Internal)
        {
            return !symbol.IsStatic
                && !symbol.IsAbstract
                && !symbol.IsIndexer
                && !symbol.IsVirtual
                && !symbol.IsWriteOnly
                && !symbol.IsImplicitlyDeclared;
        }

        return false;
    }

    public static bool IsValidForGeneration(this INamedTypeSymbol symbol)
    {
        if (symbol.IsNamespace)
            return false;

        if (symbol.DeclaredAccessibility
            is Accessibility.Public
            or Accessibility.Protected
            or Accessibility.ProtectedOrInternal
            or Accessibility.Internal)
        {
            return !symbol.IsStatic
                && !symbol.IsAbstract
                && !symbol.IsAnonymousType
                && !symbol.IsComImport
                && !symbol.IsImplicitClass
                && !symbol.IsExtern
                && !symbol.IsImplicitlyDeclared;
        }

        return false;
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

    public static IEnumerable<TResult> SelectNotNull<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
        where TResult : notnull
    {
        foreach (TSource item in source)
        {
            TResult? result = selector(item);
            if (result is not null)
                yield return result;
        }
    }
}
