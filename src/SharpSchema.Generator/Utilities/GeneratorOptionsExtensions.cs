using Microsoft.CodeAnalysis;
using SharpSchema.Annotations;

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
}
