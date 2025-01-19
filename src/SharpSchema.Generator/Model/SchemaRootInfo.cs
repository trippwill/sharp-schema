using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Annotations;

namespace SharpSchema.Generator.Model;

/// <summary>
/// The root type information for producing a schema.
/// </summary>
/// <param name="RootType">The root type of the schema.</param>
/// <param name="Filename">The filename associated with the schema.</param>
/// <param name="Id">The identifier of the schema.</param>
/// <param name="CommonNamespace">The common namespace of the schema.</param>
public record SchemaRootInfo(
        SchemaMember.Object RootType,
        string? Filename,
        string? Id,
        string? CommonNamespace)
{
    /// <summary>
    /// A visitor that processes C# syntax nodes to extract <see cref="SchemaRootInfo"/>.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SyntaxVisitor"/> class.
    /// </remarks>
    /// <param name="compilation">The compilation to use for creating semantic models.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    internal class SyntaxVisitor(Compilation compilation, CancellationToken cancellationToken)
        : CSharpSyntaxVisitor<SchemaRootInfo?>
    {
        private readonly Compilation _compilation = compilation;
        private readonly CancellationToken _cancellationToken = cancellationToken;
        private readonly SchemaMember.Object.SymbolVisitor _symbolVisitor = new(new());

        /// <summary>
        /// Visits a class declaration syntax node.
        /// </summary>
        /// <param name="node">The class declaration syntax node.</param>
        /// <returns>The extracted <see cref="SchemaRootInfo"/> or null if not applicable.</returns>
        public override SchemaRootInfo? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            SemanticModel semanticModel = _compilation.GetSemanticModel(node.SyntaxTree);
            return semanticModel.GetDeclaredSymbol(node, _cancellationToken) is INamedTypeSymbol symbol
                ? this.ProcessTypeDeclaration(symbol)
                : null;
        }

        /// <summary>
        /// Visits a struct declaration syntax node.
        /// </summary>
        /// <param name="node">The struct declaration syntax node.</param>
        /// <returns>The extracted <see cref="SchemaRootInfo"/> or null if not applicable.</returns>
        public override SchemaRootInfo? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            SemanticModel semanticModel = _compilation.GetSemanticModel(node.SyntaxTree);
            return semanticModel.GetDeclaredSymbol(node, _cancellationToken) is INamedTypeSymbol symbol
                ? this.ProcessTypeDeclaration(symbol)
                : null;
        }

        public override SchemaRootInfo? VisitCompilationUnit(CompilationUnitSyntax node)
        {
            // figure out is this unit has a schema root attribute
            return base.VisitCompilationUnit(node);
        }

        /// <summary>
        /// Processes a type declaration symbol to extract <see cref="SchemaRootInfo"/>.
        /// </summary>
        /// <param name="symbol">The type declaration symbol.</param>
        /// <returns>The extracted <see cref="SchemaRootInfo"/> or null if not applicable.</returns>
        private SchemaRootInfo? ProcessTypeDeclaration(INamedTypeSymbol symbol)
        {
            AttributeData? attributeData = symbol.GetAttributeData<SchemaRootAttribute>();
            if (attributeData is not null && symbol.Accept(_symbolVisitor) is SchemaMember.Object rootType)
            {
                string? filename = attributeData.GetNamedArgument<string>(nameof(SchemaRootAttribute.Filename));
                string? id = attributeData.GetNamedArgument<string>(nameof(SchemaRootAttribute.Id));
                string? commonNamespace = attributeData.GetNamedArgument<string>(nameof(SchemaRootAttribute.CommonNamespace));

                return new(rootType, filename, id, commonNamespace);
            }

            return null;
        }
    }
}
