using Microsoft.CodeAnalysis;
using SharpSchema.Annotations;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator.Utilities;

internal static class GeneratorOptionsExtensions
{
    public static bool ShouldProcess(this GeneratorOptions options, ISymbol symbol)
    {
        return symbol switch
        {
            IPropertySymbol property => ShouldProcessProperty(property),
            IParameterSymbol parameter => ShouldProcessParameter(parameter),
            _ => false
        };

        bool ShouldProcessParameter(IParameterSymbol symbol) => symbol.IsValidForGeneration()
            && !symbol.IsIgnoredForGeneration();

        bool ShouldProcessProperty(IPropertySymbol symbol) => symbol.IsValidForGeneration()
            && ShouldProcessAccessibility(symbol.DeclaredAccessibility, options.AccessibilityMode)
            && !symbol.IsIgnoredForGeneration();

        static bool ShouldProcessAccessibility(Accessibility accessibility, AccessibilityMode allowedAccessibilities)
        {
            return accessibility switch
            {
                Accessibility.Public => allowedAccessibilities.CheckFlag(AccessibilityMode.Public),
                Accessibility.Internal => allowedAccessibilities.CheckFlag(AccessibilityMode.Internal),
                Accessibility.Private => allowedAccessibilities.CheckFlag(AccessibilityMode.Private),
                _ => false,
            };
        }
    }

    public static GeneratorOptions Override(this GeneratorOptions options, ObjectAttributes attributes)
    {
        return new GeneratorOptions(
            AccessibilityMode: attributes.AccessibilityMode.Get<AccessibilityMode>(0) ?? options.AccessibilityMode,
            TraversalMode: attributes.TraversalMode.Get<TraversalMode>(0) ?? (options.TraversalMode),
            DictionaryKeyMode: attributes.DictionaryKeyMode.Get<DictionaryKeyMode>(0) ?? options.DictionaryKeyMode,
            EnumMode: attributes.EnumMode.Get<EnumMode>(0) ?? options.EnumMode,
            NumberMode: options.NumberMode);
    }

    public static GeneratorOptions Override(this GeneratorOptions options, PropertyAttributes attributes)
    {
        return new GeneratorOptions(
            AccessibilityMode: options.AccessibilityMode,
            TraversalMode: options.TraversalMode,
            DictionaryKeyMode: attributes.DictionaryKeyMode.Get<DictionaryKeyMode>(0) ?? options.DictionaryKeyMode,
            EnumMode: attributes.EnumMode.Get<EnumMode>(0) ?? options.EnumMode,
            NumberMode: options.NumberMode);
    }
}
