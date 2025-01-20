using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Annotations;

namespace SharpSchema.Generator.Model;

public abstract partial record SchemaMember
{
    /// <summary>
    /// Represents a schema member object.
    /// </summary>
    /// <param name="TypeSymbol">The type symbol of the object.</param>
    /// <param name="MemberData">The member data of the object.</param>
    /// <param name="Override">The override string for the object.</param>
    public abstract record Object(INamedTypeSymbol TypeSymbol, Data? MemberData, string? Override)
        : SchemaMember(MemberData, Override)
    {
        /// <summary>
        /// Represents an override object.
        /// </summary>
        /// <param name="TypeSymbol">The type symbol of the override object.</param>
        /// <param name="Override">The override string.</param>
        public record OverrideObject(INamedTypeSymbol TypeSymbol, string Override)
            : Object(TypeSymbol, MemberData: null, Override);

        /// <summary>
        /// Represents a system object.
        /// </summary>
        /// <param name="TypeSymbol">The type symbol of the system object.</param>
        /// <param name="TypeArgumentMembers">The type argument members of the system object.</param>
        public record SystemObject(
            INamedTypeSymbol TypeSymbol,
            ImmutableArray<Object> TypeArgumentMembers)
            : Object(TypeSymbol, MemberData: null, Override: null);

        /// <summary>
        /// Represents a nullable object in the schema.
        /// </summary>
        /// <param name="TypeSymbol">The type symbol of the nullable object.</param>
        /// <param name="ElementType">The element type of the nullable object.</param>
        public record NullableObject(
            INamedTypeSymbol TypeSymbol,
            Object ElementType)
            : Object(TypeSymbol, MemberData: null, Override: null);

        /// <summary>
        /// Represents a data object.
        /// </summary>
        /// <param name="TypeSymbol">The type symbol of the data object.</param>
        /// <param name="MemberData">The member data of the data object.</param>
        /// <param name="TypeArgumentMembers">The type argument members of the data object.</param>
        /// <param name="Properties">The properties of the data object.</param>
        public record DataObject(
            INamedTypeSymbol TypeSymbol,
            Data MemberData,
            ImmutableArray<Object> TypeArgumentMembers,
            ImmutableArray<Property> Properties)
            : Object(TypeSymbol, MemberData, Override: null);

        /// <summary>
        /// A syntax visitor for schema member objects.
        /// </summary>
        /// <param name="options">The options for the schema root info generator.</param>
        /// <param name="compilation">The compilation context.</param>
        internal class SyntaxVisitor(
            SchemaRootInfoGenerator.Options options,
            Compilation compilation)
            : CSharpSyntaxVisitor<Object>
        {
            /// <summary>
            /// Gets the options for the schema root info generator.
            /// </summary>
            public SchemaRootInfoGenerator.Options Options => options;

            /// <summary>
            /// Gets the compilation context.
            /// </summary>
            public Compilation Compilation => compilation;

            /// <inheritdoc />
            public override Object? DefaultVisit(SyntaxNode node) => base.DefaultVisit(node);

            /// <inheritdoc />
            public override Object? VisitClassDeclaration(ClassDeclarationSyntax node)
                => this.VisitTypeDeclaration(node);

            /// <inheritdoc />
            public override Object? VisitStructDeclaration(StructDeclarationSyntax node)
                => this.VisitTypeDeclaration(node);

            /// <inheritdoc />
            public override Object? VisitRecordDeclaration(RecordDeclarationSyntax node)
                => this.VisitTypeDeclaration(node);

            /// <inheritdoc />
            public override Object? VisitIdentifierName(IdentifierNameSyntax node)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol symbol)
                    return null;

                // if the identifier is a type identifier find the type declaration
                if (symbol.DeclaringSyntaxReferences.Length > 0)
                {
                    if (symbol.DeclaringSyntaxReferences[0].GetSyntax() is not TypeDeclarationSyntax typeDeclaration)
                        return null;

                    return typeDeclaration.Accept(this);
                }

                return null;
            }

            /// <inheritdoc />
            public override Object? VisitNullableType(NullableTypeSyntax node)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol symbol)
                    return null;

                if (node.ElementType.Accept(this) is not Object type)
                    return null;

                return new NullableObject(symbol, type);
            }

            /// <inheritdoc />
            public override Object? VisitGenericName(GenericNameSyntax node)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol symbol)
                    return null;

                ImmutableArray<Object> typeArgumentMembers = node.TypeArgumentList
                    .Arguments.SelectNotNull(tps => tps.Accept(this));

                return new SystemObject(symbol, typeArgumentMembers);
            }

            /// <inheritdoc />
            public override Object? VisitPredefinedType(PredefinedTypeSyntax node)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol symbol)
                    return null;

                return new SystemObject(symbol, []);
            }

            /// <summary>
            /// Visits a type declaration syntax node.
            /// </summary>
            /// <param name="node">The type declaration syntax node.</param>
            /// <returns>The schema member object.</returns>
            private Object? VisitTypeDeclaration(TypeDeclarationSyntax node)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(node) is not INamedTypeSymbol symbol)
                    return null;

                if (symbol.TryGetConstructorArgument<SchemaOverrideAttribute, string>(0, out string? @override))
                    return new OverrideObject(symbol, @override);

                if (!symbol.IsValidForGeneration() || symbol.IsIgnoredForGeneration())
                    return null;

                if (node.IsNestedInSystemNamespace())
                    return new SystemObject(symbol, []);

                Property.SyntaxVisitor propertySyntaxVisitor = new(this);
                ImmutableArray<Property> properties = node.Members
                    .SelectNotNull(mds => mds.Accept(propertySyntaxVisitor));

                var data = Data.SymbolVisitor.Instance.VisitNamedType(symbol);
                return new DataObject(symbol, data, [], properties);
            }
        }
    }
}
