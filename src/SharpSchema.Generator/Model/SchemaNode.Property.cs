using System.Collections.Concurrent;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Annotations;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator.Model;

public abstract partial record SchemaNode
{
    /// <summary>
    /// Represents a property in the schema member.
    /// </summary>
    /// <param name="Symbol">The symbol.</param>
    /// <param name="Metadata">The member data.</param>
    public abstract record Property(ISymbol Symbol, Metadata? Metadata = null)
        : SchemaNode(Symbol, Metadata)
    {
        /// <inheritdoc />
        public override long GetSchemaHash()
        {
            return SchemaHash.Combine(
                Symbol.GetSchemaHash(),
                Metadata?.GetSchemaHash() ?? 0,
                GetSchemaHashCore());
        }

        protected private virtual long GetSchemaHashCore() => 0;

        /// <summary>
        /// Represents an override property.
        /// </summary>
        /// <param name="Symbol">The overridden symbol.</param>
        /// <param name="SchemaString">The override schema.</param>
        public record Override(ISymbol Symbol, string SchemaString)
            : Property(Symbol)
        {
            protected private override long GetSchemaHashCore() => SchemaString.GetSchemaHash();
        }

        /// <summary>
        /// Represents a data property.
        /// </summary>
        /// <param name="Symbol">The property symbol.</param>
        /// <param name="Metadata">The member data.</param>
        /// <param name="MemberType">The member type.</param>
        /// <param name="DefaultValueSyntax">The default value syntax.</param>
        public record Custom(
            ISymbol Symbol,
            Metadata Metadata,
            Object MemberType,
            EqualsValueClauseSyntax? DefaultValueSyntax)
            : Property(Symbol, Metadata)
        {
            protected private override long GetSchemaHashCore()
            {
                return SchemaHash.Combine(
                    Metadata?.GetSchemaHash() ?? 0,
                    MemberType.GetSchemaHash(),
                    DefaultValueSyntax?.ToString().GetSchemaHash() ?? 0);
            }
        }

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

            public override Property? VisitParameter(ParameterSyntax node)
            {
                SemanticModel semanticModel = _objectSyntaxVisitor.Compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(node) is not IParameterSymbol symbol)
                    return null;

                if (symbol.TryGetConstructorArgument<SchemaOverrideAttribute, string>(0, out string? @override))
                    return new Override(symbol, @override);

                if (symbol.IsIgnoredForGeneration())
                    return null;

                Metadata data = Metadata.SymbolVisitor.Instance.VisitParameter(symbol);

                if (node.Type?.Accept(_objectSyntaxVisitor) is not Object memberType)
                    return null;

                return new Custom(symbol, data, memberType, node.Default);
            }
        }
    }
}
