using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SharpSchema.Annotations;

namespace SharpSchema.Generator.Model;

public abstract partial record SchemaNode
{
    /// <summary>
    /// Represents the different kinds of schema member objects.
    /// </summary>
    public enum ObjectKind
    {
        /// <summary>
        /// Unsupported object kind.
        /// </summary>
        Unsupported,
        /// <summary>
        /// Override object kind.
        /// </summary>
        Override,
        /// <summary>
        /// System object kind.
        /// </summary>
        System,
        /// <summary>
        /// Generic object kind.
        /// </summary>
        Generic,
        /// <summary>
        /// Map object kind.
        /// </summary>
        Map,
        /// <summary>
        /// Array object kind.
        /// </summary>
        Array,
        /// <summary>
        /// Nullable object kind.
        /// </summary>
        Nullable,
        /// <summary>
        /// Custom object kind.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Represents a schema member object.
    /// </summary>
    /// <param name="TypeSymbol">The type symbol of the object.</param>
    /// <param name="Kind">The kind of the object.</param>
    /// <param name="Metadata">The metadata of the object.</param>
    public abstract record Object(INamedTypeSymbol TypeSymbol, ObjectKind Kind, Metadata? Metadata = null)
        : SchemaNode(TypeSymbol, Metadata)
    {
        /// <summary>
        /// Represents an unsupported object.
        /// </summary>
        /// <param name="TypeSymbol">The type symbol of the unsupported object.</param>
        /// <param name="SyntaxSpan">The syntax span of the unsupported object.</param>
        public record Unsupported(INamedTypeSymbol TypeSymbol, TextSpan SyntaxSpan)
            : Object(TypeSymbol, ObjectKind.Unsupported);

        /// <summary>
        /// Represents an override object.
        /// </summary>
        /// <param name="TypeSymbol">The type symbol of the override object.</param>
        /// <param name="SchemaString">The override string.</param>
        public record Override(INamedTypeSymbol TypeSymbol, string SchemaString)
            : Object(TypeSymbol, ObjectKind.Override, Metadata: null);

        /// <summary>
        /// Represents a system object.
        /// </summary>
        /// <param name="TypeSymbol">The type symbol of the system object.</param>
        public record System(INamedTypeSymbol TypeSymbol)
            : Object(TypeSymbol, ObjectKind.System);

        /// <summary>
        /// Represents a generic object in the schema.
        /// </summary>
        /// <param name="BaseType">The base object.</param>
        /// <param name="TypeArguments">The type arguments of the generic object.</param>
        public record Generic(
            Object BaseType,
            ImmutableArray<Object> TypeArguments)
            : Object(BaseType.TypeSymbol, ObjectKind.Generic, BaseType.Metadata);

        /// <summary>
        /// Represents a dictionary object in the schema.
        /// </summary>
        /// <param name="BaseType">The base object.</param>
        /// <param name="KeyType">The key type of the dictionary object.</param>
        /// <param name="ValueType">The value type of the dictionary object.</param>
        public record Map(
            Object BaseType,
            Object KeyType,
            Object ValueType)
            : Generic(BaseType, [KeyType, ValueType]);

        /// <summary>
        /// Represents an array object in the schema.
        /// </summary>
        /// <param name="BaseType">The base object.</param>
        /// <param name="ElementType">The element type of the array object.</param>
        public record Array(
            Object BaseType,
            Object ElementType)
            : Generic(BaseType, [ElementType]);

        /// <summary>
        /// Represents a nullable object in the schema.
        /// </summary>
        /// <param name="TypeSymbol">The type symbol of the nullable object.</param>
        /// <param name="ElementType">The element type of the nullable object.</param>
        public record Nullable(
            INamedTypeSymbol TypeSymbol,
            Object ElementType)
            : Object(TypeSymbol, ObjectKind.Nullable);

        /// <summary>
        /// Represents a custom data object.
        /// </summary>
        /// <param name="TypeSymbol">The type symbol of the data object.</param>
        /// <param name="Metadata">The metadata of the data object.</param>
        /// <param name="Properties">The properties of the data object.</param>
        public record Custom(
            INamedTypeSymbol TypeSymbol,
            Metadata? Metadata,
            ImmutableArray<Property> Properties)
            : Object(TypeSymbol, ObjectKind.Custom, Metadata);

        /// <summary>
        /// A syntax visitor for schema member objects.
        /// </summary>
        /// <param name="options">The options for the schema root info generator.</param>
        /// <param name="compilation">The compilation context.</param>
        internal class SyntaxVisitor(SchemaTreeGenerator.Options options, Compilation compilation)
            : CSharpSyntaxVisitor<Object>
        {
            private static readonly string DictionaryMetadataName = typeof(IDictionary<,>).FullName!;
            private static readonly string EnumerableMetadataName = typeof(IEnumerable<>).FullName!;

            private readonly ConcurrentDictionary<ISymbol, Object> _cache = new(SymbolEqualityComparer.IncludeNullability);

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
                return new Unsupported(
                    Compilation.GetSpecialType(SpecialType.System_Void),
                    node.Span);
            }

            /// <inheritdoc />
            public override Object? VisitClassDeclaration(ClassDeclarationSyntax node)
            {
                return VisitTypeDeclaration(node, AllowedTypeDeclarations.Class);
            }

            /// <inheritdoc />
            public override Object? VisitStructDeclaration(StructDeclarationSyntax node)
            {
                return VisitTypeDeclaration(node, AllowedTypeDeclarations.Struct);
            }

            /// <inheritdoc />
            public override Object? VisitRecordDeclaration(RecordDeclarationSyntax node)
            {
                bool isStruct = node.ClassOrStructKeyword.Text == "struct";
                return VisitTypeDeclaration(
                    node,
                    isStruct
                        ? AllowedTypeDeclarations.Struct
                        : AllowedTypeDeclarations.Class);
            }

            /// <inheritdoc />
            public override Object? VisitIdentifierName(IdentifierNameSyntax node)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol symbol)
                    return null;

                if (_cache.TryGetValue(symbol, out var cachedObject))
                    return cachedObject;

                if (!symbol.ShouldProcessAccessibility(options))
                    return null;

                // if the identifier is a type identifier find the type declaration
                if (symbol.DeclaringSyntaxReferences.Length > 0)
                {
                    if (symbol.DeclaringSyntaxReferences[0].GetSyntax() is not TypeDeclarationSyntax typeDeclaration)
                        return null;

                    if (typeDeclaration.Accept(this) is Object result)
                        return _cache[symbol] = result;
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

                return new Nullable(symbol, type);
            }

            /// <inheritdoc />
            public override Object? VisitArrayType(ArrayTypeSyntax node)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol symbol)
                    return null;

                if (_cache.TryGetValue(symbol, out var cachedObject))
                    return cachedObject;

                if (node.ElementType.Accept(this) is not Object elementType)
                    return null;

                return _cache[symbol] = new Array(new System(symbol), elementType);
            }

            /// <inheritdoc />
            public override Object? VisitGenericName(GenericNameSyntax node)
            {
                if (node.IsUnboundGenericName)
                    return null;

                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol boundSymbol)
                    return null;

                if (_cache.TryGetValue(boundSymbol, out var cachedObject))
                    return cachedObject;

                if (!boundSymbol.ShouldProcessAccessibility(options))
                    return null;

                INamedTypeSymbol baseSymbol = boundSymbol.ConstructedFrom;

                // If the symbol has no declaration, it is a system type like Dictionary<,> or IEnumerable<>
                Object? baseType = baseSymbol.FindTypeDeclaration() is not TypeDeclarationSyntax baseTypeDeclaration
                    ? new System(baseSymbol)
                    : baseTypeDeclaration.Accept(this);

                if (baseType is null)
                    return null;

                INamedTypeSymbol unboundDictionarySymbol = compilation.GetTypeByMetadataName(DictionaryMetadataName)
                    ?? throw new InvalidOperationException($"{DictionaryMetadataName} not found in compilation.");

                INamedTypeSymbol? boundDictionarySymbol = boundSymbol.ImplementsGenericInterface(unboundDictionarySymbol);
                if (boundDictionarySymbol is not null)
                {
                    ImmutableArray<Object> typeArgumentMembers = node.TypeArgumentList
                        .Arguments.SelectNotNull(tps => tps.Accept(this));

                    if (typeArgumentMembers.Length == 2)
                        return _cache[boundSymbol] = new Map(baseType, typeArgumentMembers[0], typeArgumentMembers[1]);
                }

                INamedTypeSymbol unboundEnumerableSymbol = compilation.GetTypeByMetadataName(EnumerableMetadataName)
                    ?? throw new InvalidOperationException($"{EnumerableMetadataName} not found in compilation.");

                INamedTypeSymbol? boundEnumerableSymbol = boundSymbol.ImplementsGenericInterface(unboundEnumerableSymbol);
                if (boundEnumerableSymbol is not null)
                {
                    ImmutableArray<Object> typeArgumentMembers = node.TypeArgumentList
                        .Arguments.SelectNotNull(tps => tps.Accept(this));

                    if (typeArgumentMembers.Length == 1)
                        return _cache[boundSymbol] = new Array(baseType, typeArgumentMembers[0]);
                }

                if (boundSymbol.TypeArguments.Length > 0)
                {
                    ImmutableArray<Object> typeArgumentMembers = node.TypeArgumentList
                        .Arguments.SelectNotNull(tps => tps.Accept(this));

                    return _cache[boundSymbol] = new Generic(baseType, typeArgumentMembers);
                }

                return null;
            }

            /// <inheritdoc />
            public override Object? VisitPredefinedType(PredefinedTypeSyntax node)
            {
                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetTypeInfo(node).Type is not INamedTypeSymbol symbol)
                    return null;

                if (_cache.TryGetValue(symbol, out var cachedObject))
                    return cachedObject;

                return _cache[symbol] = new System(symbol);
            }

            /// <summary>
            /// Visits a type declaration syntax node.
            /// </summary>
            /// <param name="node">The type declaration syntax node.</param>
            /// <param name="allowedType">The allowed type declaration.</param>
            /// <returns>The schema member object.</returns>
            private Object? VisitTypeDeclaration(TypeDeclarationSyntax node, AllowedTypeDeclarations allowedType)
            {
                if (!options.TypeOptions.AllowedTypeDeclarations.CheckFlag(allowedType))
                    return null;

                SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
                if (semanticModel.GetDeclaredSymbol(node) is not INamedTypeSymbol symbol)
                    return null;

                if (_cache.TryGetValue(symbol, out var cachedObject))
                    return cachedObject;

                if (symbol.TryGetConstructorArgument<SchemaOverrideAttribute, string>(0, out string? @override))
                    return _cache[symbol] = new Override(symbol, @override);

                if (!symbol.ShouldProcessAccessibility(options))
                    return null;

                if (!symbol.IsValidForGeneration() || symbol.IsIgnoredForGeneration())
                    return null;

                if (node.IsNestedInSystemNamespace())
                    return _cache[symbol] = new System(symbol);

                Property.SyntaxVisitor propertyVisitor = Property.SyntaxVisitor.GetInstance(this);
                ImmutableArray<Property> properties = node.Members
                    .SelectNotNull(mds => mds.Accept(propertyVisitor));

                Metadata data = Metadata.SymbolVisitor.Instance.VisitNamedType(symbol);
                return _cache[symbol] = new Custom(symbol, data, properties);
            }
        }
    }
}
