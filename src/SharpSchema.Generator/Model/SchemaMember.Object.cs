using System.Collections.Concurrent;
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
        public record SystemObject(
            INamedTypeSymbol TypeSymbol)
            : Object(TypeSymbol, MemberData: null, Override: null);

        /// <summary>
        /// Represents a generic object in the schema.
        /// </summary>
        /// <param name="TypeSymbol">The type symbol of the generic object.</param>
        /// <param name="Arity">The arity of the generic object.</param>
        /// <param name="TypeArgumentMembers">The type argument members of the generic object.</param>
        public record GenericObject(
            INamedTypeSymbol TypeSymbol,
            int Arity,
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
        /// <param name="Properties">The properties of the data object.</param>
        public record DataObject(
            INamedTypeSymbol TypeSymbol,
            Data MemberData,
            ImmutableArray<Property> Properties)
            : Object(TypeSymbol, MemberData, Override: null);

        /// <summary>
        /// A syntax visitor for schema member objects.
        /// </summary>
        /// <param name="options">The options for the schema root info generator.</param>
        /// <param name="compilation">The compilation context.</param>
        internal class SyntaxVisitor(
            SchemaTreeGenerator.Options options,
            Compilation compilation)
            : CSharpSyntaxVisitor<Object>
        {
            private readonly ConcurrentDictionary<SyntaxNode, Object> _cache = new();

            /// <summary>
            /// Gets the options for the schema root info generator.
            /// </summary>
            public SchemaTreeGenerator.Options Options => options;

            /// <summary>
            /// Gets the compilation context.
            /// </summary>
            public Compilation Compilation => compilation;

            /// <inheritdoc />
            public override Object? DefaultVisit(SyntaxNode node)
            {
                // TODO: Log warning
                return null;
            }

            /// <inheritdoc />
            public override Object? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                if (!options.TypeOptions.AllowedTypeDeclarations.CheckFlag(AllowedTypeDeclarations.Class))
                    return null;

                return this.VisitTypeDeclaration(node);
            }

            /// <inheritdoc />
            public override Object? VisitStructDeclaration(StructDeclarationSyntax node)
            {
                if (!options.TypeOptions.AllowedTypeDeclarations.CheckFlag(AllowedTypeDeclarations.Struct))
                    return null;

                return this.VisitTypeDeclaration(node);
            }

            /// <inheritdoc />
            public override Object? VisitRecordDeclaration(RecordDeclarationSyntax node)
            {
                if (!options.TypeOptions.AllowedTypeDeclarations.CheckFlag(AllowedTypeDeclarations.Record))
                    return null;

                bool isStruct = node.ClassOrStructKeyword.Text == "struct";
                if (isStruct && !options.TypeOptions.AllowedTypeDeclarations.CheckFlag(AllowedTypeDeclarations.Struct))
                    return null;

                if (!isStruct && !options.TypeOptions.AllowedTypeDeclarations.CheckFlag(AllowedTypeDeclarations.Class))
                    return null;

                return this.VisitTypeDeclaration(node);
            }

            /// <inheritdoc />
            public override Object? VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (_cache.TryGetValue(node, out var cachedObject))
                {
                    return cachedObject;
                }

                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol symbol)
                    return null;

                if (!symbol.ShouldProcessAccessibility(options))
                    return null;

                // if the identifier is a type identifier find the type declaration
                if (symbol.DeclaringSyntaxReferences.Length > 0)
                {
                    if (symbol.DeclaringSyntaxReferences[0].GetSyntax() is not TypeDeclarationSyntax typeDeclaration)
                        return null;

                    if (typeDeclaration.Accept(this) is Object result)
                    {
                        return _cache[node] = result;
                    }
                }

                return null;
            }

            /// <inheritdoc />
            public override Object? VisitNullableType(NullableTypeSyntax node)
            {
                if (_cache.TryGetValue(node, out var cachedObject))
                {
                    return cachedObject;
                }

                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol symbol)
                    return null;

                if (node.ElementType.Accept(this) is not Object type)
                    return null;

                var result = new NullableObject(symbol, type);
                _cache[node] = result;
                return result;
            }

            /// <inheritdoc />
            public override Object? VisitGenericName(GenericNameSyntax node)
            {
                if (node.IsUnboundGenericName)
                    return null;

                if (_cache.TryGetValue(node, out var cachedObject))
                    return cachedObject;

                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol symbol)
                    return null;

                if (!symbol.ShouldProcessAccessibility(options))
                    return null;

                if (symbol.TypeArguments.Length > 0)
                {
                    ImmutableArray<Object> typeArgumentMembers = node.TypeArgumentList
                        .Arguments.SelectNotNull(tps => tps.Accept(this));

                    return _cache[node] = new GenericObject(symbol, symbol.Arity, typeArgumentMembers);
                }

                return null;
            }

            /// <inheritdoc />
            public override Object? VisitPredefinedType(PredefinedTypeSyntax node)
            {
                if (_cache.TryGetValue(node, out var cachedObject))
                {
                    return cachedObject;
                }

                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol symbol)
                    return null;

                var result = new SystemObject(symbol);
                _cache[node] = result;
                return result;
            }

            /// <summary>
            /// Visits a type declaration syntax node.
            /// </summary>
            /// <param name="node">The type declaration syntax node.</param>
            /// <returns>The schema member object.</returns>
            private Object? VisitTypeDeclaration(TypeDeclarationSyntax node)
            {
                if (_cache.TryGetValue(node, out var cachedObject))
                {
                    return cachedObject;
                }

                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(node) is not INamedTypeSymbol symbol)
                    return null;

                if (symbol.TryGetConstructorArgument<SchemaOverrideAttribute, string>(0, out string? @override))
                {
                    var result = new OverrideObject(symbol, @override);
                    _cache[node] = result;
                    return result;
                }

                if (!symbol.ShouldProcessAccessibility(options))
                    return null;

                if (!symbol.IsValidForGeneration() || symbol.IsIgnoredForGeneration())
                    return null;

                if (node.IsNestedInSystemNamespace())
                {
                    var result = new SystemObject(symbol);
                    _cache[node] = result;
                    return result;
                }

                Property.SyntaxVisitor propertyVisitor = Property.SyntaxVisitor.GetInstance(this);
                ImmutableArray<Property> properties = node.Members
                    .SelectNotNull(mds => mds.Accept(propertyVisitor));

                var data = Data.SymbolVisitor.Instance.VisitNamedType(symbol);
                var dataObject = new DataObject(symbol, data, properties);
                _cache[node] = dataObject;
                return dataObject;
            }
        }
    }
}
