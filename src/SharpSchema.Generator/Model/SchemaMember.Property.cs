using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Annotations;

namespace SharpSchema.Generator.Model;

public abstract partial record SchemaMember
{
    /// <summary>
    /// Represents a property in the schema member.
    /// </summary>
    /// <param name="PropertySymbol">The property symbol.</param>
    /// <param name="MemberData">The member data.</param>
    /// <param name="Override">The override string.</param>
    public abstract record Property(IPropertySymbol PropertySymbol, Data? MemberData, string? Override)
        : SchemaMember(MemberData, Override)
    {
        /// <summary>
        /// Represents an override property.
        /// </summary>
        /// <param name="PropertySymbol">The property symbol.</param>
        /// <param name="Override">The override string.</param>
        public record OverrideProperty(IPropertySymbol PropertySymbol, string Override)
            : Property(PropertySymbol, MemberData: null, Override);

        /// <summary>
        /// Represents a data property.
        /// </summary>
        /// <param name="PropertySymbol">The property symbol.</param>
        /// <param name="MemberData">The member data.</param>
        /// <param name="MemberType">The member type.</param>
        /// <param name="DefaultValueSyntax">The default value syntax.</param>
        public record DataProperty(
            IPropertySymbol PropertySymbol,
            Data MemberData,
            Object MemberType,
            EqualsValueClauseSyntax? DefaultValueSyntax)
            : Property(PropertySymbol, MemberData, Override: null);

        /// <summary>
        /// A syntax visitor for properties.
        /// </summary>
        /// <param name="objectSyntaxVisitor">The object syntax visitor.</param>
        internal class SyntaxVisitor(Object.SyntaxVisitor objectSyntaxVisitor)
            : CSharpSyntaxVisitor<Property>
        {
            /// <summary>
            /// Visits a property declaration syntax node.
            /// </summary>
            /// <param name="node">The property declaration syntax node.</param>
            /// <returns>The property.</returns>
            public override Property? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                SemanticModel semanticModel = objectSyntaxVisitor.Compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(node) is not IPropertySymbol symbol)
                    return null;

                if (!symbol.ShouldProcessAccessibility(objectSyntaxVisitor.Options))
                    return null;

                if (symbol.TryGetConstructorArgument<SchemaOverrideAttribute, string>(0, out string? @override))
                    return new OverrideProperty(symbol, @override);

                if (!symbol.IsValidForGeneration() || symbol.IsIgnoredForGeneration())
                    return null;

                var data = Data.SymbolVisitor.Instance.VisitProperty(symbol);

                if (node.Type.Accept(objectSyntaxVisitor) is not Object memberType)
                    return null;

                return new DataProperty(symbol, data, memberType, node.Initializer);
            }
        }
    }
}
