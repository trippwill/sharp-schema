using Microsoft.CodeAnalysis;

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
            && ShouldProcessAccessibility(symbol.DeclaredAccessibility, options.Accessibilities)
            && !symbol.IsIgnoredForGeneration();

        static bool ShouldProcessAccessibility(Accessibility accessibility, Accessibilities allowedAccessibilities)
        {
            return accessibility switch
            {
                Accessibility.Public => allowedAccessibilities.CheckFlag(Accessibilities.Public),
                Accessibility.Internal => allowedAccessibilities.CheckFlag(Accessibilities.Internal),
                Accessibility.Private => allowedAccessibilities.CheckFlag(Accessibilities.Private),
                _ => false,
            };
        }
    }
}
