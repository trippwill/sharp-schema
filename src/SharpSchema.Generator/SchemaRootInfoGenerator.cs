using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator;

/// <summary>
/// Generates schema root information.
/// </summary>
public class SchemaRootInfoGenerator(SchemaRootInfoGenerator.Options? options = null)
{
    private readonly Options _options = options ?? Options.Default;

    /// <summary>
    /// Options for type declarations.
    /// </summary>
    public record TypeOptions(
        AllowedTypeDeclarations AllowedTypeDeclarations = AllowedTypeDeclarations.Any,
        AllowedAccessibilities AllowedAccessibilities = AllowedAccessibilities.Default);

    /// <summary>
    /// Options for member declarations.
    /// </summary>
    public record MemberOptions(
        AllowedAccessibilities AllowedAccessibilities = AllowedAccessibilities.Default);

    /// <summary>
    /// Options for the schema root info generator.
    /// </summary>
    public record Options(
        TypeOptions TypeOptions,
        MemberOptions MemberOptions)
    {
        /// <summary>
        /// Gets the default options.
        /// </summary>
        public static Options Default { get; } = new(
            new TypeOptions(),
            new MemberOptions());

        /// <summary>
        /// Returns a string representation of the options.
        /// </summary>
        /// <returns>A string representation of the options.</returns>
        public override string ToString() => $"{TypeOptions.AllowedAccessibilities}[{TypeOptions.AllowedTypeDeclarations}]_{MemberOptions.AllowedAccessibilities}";
    }

    /// <summary>
    /// Finds schema root types in the given workspace.
    /// </summary>
    /// <param name="workspace">The workspace to search for schema root types.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A collection of <see cref="SchemaRootInfo"/>.</returns>
    [ExcludeFromCodeCoverage]
    public async Task<IReadOnlyCollection<SchemaRootInfo>> FindRootsAsync(
        Workspace workspace,
        CancellationToken cancellationToken)
    {
        Throw.IfNullArgument(workspace, nameof(workspace));

        List<SchemaRootInfo> schemaRootInfos = [];
        foreach (Project project in workspace.CurrentSolution.Projects)
        {
            IReadOnlyCollection<SchemaRootInfo> projectRootInfos = await this.FindRootsAsync(project, cancellationToken);
            schemaRootInfos.AddRange(projectRootInfos);
        }

        return schemaRootInfos;
    }

    /// <summary>
    /// Finds schema root types in the given project.
    /// </summary>
    /// <param name="project">The project to search for schema root types.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A collection of <see cref="SchemaRootInfo"/>.</returns>
    public async Task<IReadOnlyCollection<SchemaRootInfo>> FindRootsAsync(
        Project project,
        CancellationToken cancellationToken)
    {
        Throw.IfNullArgument(project, nameof(project));

        if (await project.GetCompilationAsync(cancellationToken) is not Compilation compilation)
            return [];

        List<SchemaRootInfo> schemaRootInfos = [];
        foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
        {
            IReadOnlyCollection<SchemaRootInfo> syntaxTreeRootInfos = await this.FindRootsAsync(syntaxTree, compilation, cancellationToken);
            schemaRootInfos.AddRange(syntaxTreeRootInfos);
        }

        return schemaRootInfos;
    }

    /// <summary>
    /// Finds schema root types in the given syntax tree.
    /// </summary>
    /// <param name="syntaxTree">The syntax tree to search for schema root types.</param>
    /// <param name="compilation">The compilation containing the syntax tree.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A collection of <see cref="SchemaRootInfo"/>.</returns>
    public async Task<IReadOnlyCollection<SchemaRootInfo>> FindRootsAsync(
        SyntaxTree syntaxTree,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        Throw.IfNullArgument(syntaxTree, nameof(syntaxTree));
        Throw.IfNullArgument(compilation, nameof(compilation));

        List<SchemaRootInfo> schemaRootInfos = [];
        SchemaRootInfoSyntaxWalker rootSyntaxWalker = new(
            _options,
            compilation,
            schemaRootInfos);

        SyntaxNode root = await syntaxTree.GetRootAsync(cancellationToken);
        rootSyntaxWalker.Visit(root);

        return schemaRootInfos;
    }

    /// <summary>
    /// Filters the SyntaxTree based on the options.
    /// </summary>
    internal class SchemaRootInfoSyntaxWalker(
        Options options,
        Compilation compilation,
        List<SchemaRootInfo> schemaRootInfos)
        : CSharpSyntaxWalker
    {
        private readonly SchemaMember.Object.SyntaxVisitor _objectVisitor = new(options, compilation);

        /// <summary>
        /// Visits a class declaration syntax node.
        /// </summary>
        /// <param name="node">The class declaration syntax node.</param>
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (!options.TypeOptions.AllowedTypeDeclarations.HasFlag(AllowedTypeDeclarations.Class))
                return;

            this.ProcessTypeDeclaration(node);
        }

        /// <summary>
        /// Visits a struct declaration syntax node.
        /// </summary>
        /// <param name="node">The struct declaration syntax node.</param>
        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (!options.TypeOptions.AllowedTypeDeclarations.HasFlag(AllowedTypeDeclarations.Struct))
                return;

            this.ProcessTypeDeclaration(node);
        }

        /// <summary>
        /// Visits a record declaration syntax node.
        /// </summary>
        /// <param name="node">The record declaration syntax node.</param>
        public override void VisitRecordDeclaration(RecordDeclarationSyntax node)
        {
            if (!options.TypeOptions.AllowedTypeDeclarations.HasFlag(AllowedTypeDeclarations.Record))
                return;

            bool isStruct = node.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword);
            bool isClass = node.ClassOrStructKeyword.IsKind(SyntaxKind.ClassKeyword) || node.ClassOrStructKeyword.IsKind(SyntaxKind.None);

            if (isStruct && !options.TypeOptions.AllowedTypeDeclarations.HasFlag(AllowedTypeDeclarations.Struct))
                return;

            if (isClass && !options.TypeOptions.AllowedTypeDeclarations.HasFlag(AllowedTypeDeclarations.Class))
                return;

            this.ProcessTypeDeclaration(node);
        }

        /// <summary>
        /// Processes a type declaration syntax node.
        /// </summary>
        /// <param name="node">The type declaration syntax node.</param>
        private void ProcessTypeDeclaration(TypeDeclarationSyntax node)
        {
            AttributeSyntax? schemaRootAttribute = node.AttributeLists
                .SelectMany(list => list.Attributes)
                .FirstOrDefault(attribute => attribute.Name.ToString() == "SchemaRoot");

            if (schemaRootAttribute is null)
                return;

            if (node.Accept(_objectVisitor) is not SchemaMember.Object rootType)
                return;

            schemaRootInfos.Add(GetSchemaRootInfo(rootType, schemaRootAttribute));
        }

        /// <summary>
        /// Gets the schema root information from the specified root type and attribute syntax.
        /// </summary>
        /// <param name="rootType">The root type.</param>
        /// <param name="attributeSyntax">The attribute syntax.</param>
        /// <returns>The schema root information.</returns>
        private static SchemaRootInfo GetSchemaRootInfo(SchemaMember.Object rootType, AttributeSyntax attributeSyntax)
        {
            string? filename = null;
            string? id = null;
            string? commonNamespace = null;

            foreach (AttributeArgumentSyntax argument in attributeSyntax.ArgumentList?.Arguments ?? [])
            {
                if (argument.NameEquals?.Name.ToString() is not string argumentName)
                    continue;

                string argumentValue = argument.Expression.ToString();

                switch (argumentName)
                {
                    case "Filename":
                        filename = argumentValue;
                        break;
                    case "Id":
                        id = argumentValue;
                        break;
                    case "CommonNamespace":
                        commonNamespace = argumentValue;
                        break;
                }
            }

            return new(rootType, filename, id, commonNamespace);
        }
    }
}
