using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SharpSchema.Annotations;
using SharpSchema.Generator.Utilities;

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
        /// Abstract object kind.
        /// </summary>
        Abstract,
        /// <summary>
        /// Nullable object kind.
        /// </summary>
        Nullable,
        /// <summary>
        /// Custom object kind.
        /// </summary>
        Custom,
        /// <summary>
        /// Tuple object kind.
        /// </summary>
        Tuple,
    }

    /// <summary>
    /// Represents a schema member object.
    /// </summary>
    /// <param name="Symbol">The type symbol of the object.</param>
    /// <param name="Kind">The kind of the object.</param>
    /// <param name="Metadata">The metadata of the object.</param>
    public abstract record Object(ISymbol Symbol, ObjectKind Kind, Metadata? Metadata = null)
        : SchemaNode(Symbol, Metadata)
    {
        /// <inheritdoc />
        public override long GetSchemaHash() => SchemaHash.Combine(
            Symbol.GetSchemaHash(),
            (long)Kind,
            Metadata?.GetSchemaHash() ?? 0,
            GetSchemaHashCore());

        protected private virtual long GetSchemaHashCore() => 0;

        /// <summary>
        /// Represents an unsupported object.
        /// </summary>
        /// <param name="Symbol">The type symbol of the unsupported object.</param>
        /// <param name="SyntaxSpan">The syntax span of the unsupported object.</param>
        public record Unsupported(ISymbol Symbol, TextSpan SyntaxSpan)
            : Object(Symbol, ObjectKind.Unsupported)
        {
            protected private override long GetSchemaHashCore() => SyntaxSpan.ToString().GetSchemaHash();
        }

        /// <summary>
        /// Represents an override object.
        /// </summary>
        /// <param name="Symbol">The type symbol of the override object.</param>
        /// <param name="SchemaString">The override string.</param>
        public record Override(ISymbol Symbol, string SchemaString)
            : Object(Symbol, ObjectKind.Override, Metadata: null)
        {
            protected private override long GetSchemaHashCore() => SchemaString.GetSchemaHash();
        }

        /// <summary>
        /// Represents a system object.
        /// </summary>
        /// <param name="Symbol">The type symbol of the system object.</param>
        public record System(ISymbol Symbol)
            : Object(Symbol, ObjectKind.System)
        {
            protected private override long GetSchemaHashCore() => 8675309;
        }

        /// <summary>
        /// Represents an abstract object in the schema.
        /// </summary>
        /// <param name="Symbol">The type symbol of the abstract object.</param>
        /// <param name="Implementations">The implementations of the abstract object.</param>
        public record Abstract(ISymbol Symbol, StructuralArray<Object> Implementations)
            : Object(Symbol, ObjectKind.Abstract)
        {
            protected private override long GetSchemaHashCore() => Implementations.GetSchemaHash();
        }

        /// <summary>
        /// Represents a generic object in the schema.
        /// </summary>
        /// <param name="BaseType">The base object.</param>
        /// <param name="TypeArguments">The type arguments of the generic object.</param>
        /// <param name="Kind">The specialized kind of the generic object.</param>
        public record Generic(Object BaseType, StructuralArray<Object> TypeArguments, ObjectKind Kind = ObjectKind.Generic)
            : Object(BaseType.Symbol, Kind, BaseType.Metadata)
        {
            protected private override long GetSchemaHashCore() => SchemaHash.Combine(
                BaseType.GetSchemaHash(),
                TypeArguments.GetSchemaHash());
        }

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
            : Generic(BaseType, [KeyType, ValueType], ObjectKind.Map);

        /// <summary>
        /// Represents an array object in the schema.
        /// </summary>
        /// <param name="BaseType">The base object.</param>
        /// <param name="ElementType">The element type of the array object.</param>
        public record Array(Object BaseType, Object ElementType)
            : Generic(BaseType, [ElementType], ObjectKind.Array);

        /// <summary>
        /// Represents a nullable object in the schema.
        /// </summary>
        /// <param name="Symbol">The type symbol of the nullable object.</param>
        /// <param name="ElementType">The element type of the nullable object.</param>
        public record Nullable(ISymbol Symbol, Object ElementType)
            : Object(Symbol, ObjectKind.Nullable)
        {
            protected private override long GetSchemaHashCore() => ElementType.GetSchemaHash();
        }

        /// <summary>
        /// Represents a custom data object.
        /// </summary>
        /// <param name="Symbol">The type symbol of the data object.</param>
        /// <param name="Metadata">The metadata of the data object.</param>
        /// <param name="Properties">The properties of the data object.</param>
        public record Custom(
            ISymbol Symbol,
            Metadata? Metadata,
            StructuralArray<Property> Properties)
            : Object(Symbol, ObjectKind.Custom, Metadata)
        {
            protected private override long GetSchemaHashCore() => Properties.GetSchemaHash();
        }

        /// <summary>
        /// Represents a tuple object in the schema.
        /// </summary>
        /// <param name="Elements">The elements of the tuple.</param>
        public record Tuple(StructuralArray<Object> Elements)
            : Object(Elements.First().Symbol, ObjectKind.Tuple)
        {
            protected private override long GetSchemaHashCore() => Elements.GetSchemaHash();
        }

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
                return VisitTypeDeclaration(node);
            }

            /// <inheritdoc />
            public override Object? VisitStructDeclaration(StructDeclarationSyntax node)
            {
                return VisitTypeDeclaration(node);
            }

            /// <inheritdoc />
            public override Object? VisitRecordDeclaration(RecordDeclarationSyntax node)
            {
                bool isStruct = node.ClassOrStructKeyword.Text == "struct";
                return VisitTypeDeclaration(node);
            }

            public override Object? VisitParameter(ParameterSyntax node) => base.VisitParameter(node);

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
                if (node.ElementType.Accept(this) is not Object elementType)
                    return null;

                return new Array(new System(Compilation.GetSpecialType(SpecialType.System_Array)), elementType);
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

            /// <inheritdoc />
            public override Object? VisitTupleType(TupleTypeSyntax node)
            {
                ImmutableArray<Object> elements = node.Elements
                    .SelectNotNull(e => e.Type.Accept(this));

                if (elements.Length == 0)
                    return null;

                return new Tuple(elements);
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

                if (_cache.TryGetValue(symbol, out var cachedObject))
                    return cachedObject;

                if (symbol.TryGetConstructorArgument<SchemaOverrideAttribute, string>(0, out string? @override))
                    return _cache[symbol] = new Override(symbol, @override);

                if (!symbol.ShouldProcessAccessibility(options))
                    return null;

                if (!symbol.IsValidForGeneration() || symbol.IsIgnoredForGeneration())
                    return null;

                if (symbol.IsAbstract)
                {
                    ImmutableArray<Object> implementations = FindImplementations(symbol)
                        .SelectNotNull(this.Visit);

                    return _cache[symbol] = new Abstract(symbol, implementations);
                }

                if (node.IsNestedInSystemNamespace())
                    return _cache[symbol] = new System(symbol);

                Property.SyntaxVisitor propertyVisitor = Property.SyntaxVisitor.GetInstance(this);

                ImmutableArray<Property>.Builder propertiesBuilder = ImmutableArray.CreateBuilder<Property>();
                propertiesBuilder.AddRange(node.Members.SelectNotNull(mds => mds.Accept(propertyVisitor)));

                if (node.ParameterList is ParameterListSyntax parameterList)
                {
                    propertiesBuilder.AddRange(parameterList.Parameters.SelectNotNull(p => p.Accept(propertyVisitor)));
                }

                ImmutableArray<Property> properties = propertiesBuilder.ToImmutable();
                Metadata data = Metadata.SymbolVisitor.Instance.VisitNamedType(symbol);
                return _cache[symbol] = new Custom(symbol, data, properties);
            }

            /// <summary>
            /// Finds all syntax nodes that implement the given abstract symbol.
            /// </summary>
            /// <param name="abstractSymbol">The abstract symbol to find implementations for.</param>
            /// <returns>A list of syntax nodes that implement the given abstract symbol.</returns>
            private IEnumerable<SyntaxNode> FindImplementations(INamedTypeSymbol abstractSymbol)
            {
                foreach (var tree in compilation.SyntaxTrees)
                {
                    var root = tree.GetRoot();
                    var semanticModel = compilation.GetSemanticModel(tree);
                    var nodes = root.DescendantNodes().OfType<TypeDeclarationSyntax>();

                    foreach (var node in nodes)
                    {
                        if (semanticModel.GetDeclaredSymbol(node) is INamedTypeSymbol symbol
                            && symbol.ImplementsAbstractClass(abstractSymbol))
                        {
                            yield return node;
                        }
                    }
                }
            }
        }
    }
}
