using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Annotations;

namespace SharpSchema.Generator.Model;

public abstract partial record SchemaNode
{
    /// <summary>
    /// Represents a property in the schema member.
    /// </summary>
    /// <param name="PropertySymbol">The property symbol.</param>
    /// <param name="Metadata">The member data.</param>
    public abstract record Property(IPropertySymbol PropertySymbol, Metadata? Metadata = null)
        : SchemaNode(PropertySymbol, Metadata)
    {
        /// <summary>
        /// Represents an override property.
        /// </summary>
        /// <param name="PropertySymbol">The property symbol.</param>
        /// <param name="SchemaString">The override string.</param>
        public record Override(IPropertySymbol PropertySymbol, string SchemaString)
            : Property(PropertySymbol);

        /// <summary>
        /// Represents a data property.
        /// </summary>
        /// <param name="PropertySymbol">The property symbol.</param>
        /// <param name="Metadata">The member data.</param>
        /// <param name="MemberType">The member type.</param>
        /// <param name="DefaultValueSyntax">The default value syntax.</param>
        public record Custom(
            IPropertySymbol PropertySymbol,
            Metadata Metadata,
            Object MemberType,
            EqualsValueClauseSyntax? DefaultValueSyntax)
            : Property(PropertySymbol, Metadata);

        /// <summary>
        /// A syntax visitor for properties.
        /// </summary>
        internal class SyntaxVisitor
            : CSharpSyntaxVisitor<Property>
        {
            private static readonly ConcurrentDictionary<Object.SyntaxVisitor, SyntaxVisitor> _instanceCache = new();

            private readonly Object.SyntaxVisitor _objectSyntaxVisitor;

            private SyntaxVisitor(Object.SyntaxVisitor objectSyntaxVisitor)
            {
                _objectSyntaxVisitor = objectSyntaxVisitor;
            }

            public static SyntaxVisitor GetInstance(Object.SyntaxVisitor objectSyntaxVisitor)
            {
                return _instanceCache.GetOrAdd(objectSyntaxVisitor, _ => new SyntaxVisitor(_));
            }

            /// <summary>
            /// Visits a property declaration syntax node.
            /// </summary>
            /// <param name="node">The property declaration syntax node.</param>
            /// <returns>The property.</returns>
            public override Property? VisitPropertyDeclaration(PropertyDeclarationSyntax node)
            {
                return CreateProperty(node);

                Property? CreateProperty(PropertyDeclarationSyntax node)
                {
                    SemanticModel semanticModel = _objectSyntaxVisitor.Compilation.GetSemanticModel(node.SyntaxTree);
                    if (semanticModel.GetDeclaredSymbol(node) is not IPropertySymbol symbol)
                        return null;

                    if (!symbol.ShouldProcessAccessibility(_objectSyntaxVisitor.Options))
                        return null;

                    if (symbol.TryGetConstructorArgument<SchemaOverrideAttribute, string>(0, out string? @override))
                        return new Override(symbol, @override);

                    if (!symbol.IsValidForGeneration() || symbol.IsIgnoredForGeneration())
                        return null;

                    Metadata data = Metadata.SymbolVisitor.Instance.VisitProperty(symbol);

                    if (node.Type.Accept(_objectSyntaxVisitor) is not Object memberType)
                        return null;

                    return new Custom(symbol, data, memberType, node.Initializer);
                }
            }
        }
    }
}
