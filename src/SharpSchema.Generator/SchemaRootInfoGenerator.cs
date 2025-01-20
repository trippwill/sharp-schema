using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Annotations;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[Flags]
public enum AllowedTypeDeclarations
{
    Class = 1,
    Struct = 2,
    Record = 4,
    Any = Class | Struct | Record,
    Default = Any,
}

[Flags]
public enum AllowedAccessibilities
{
    Public = 1,
    Internal = 2,
    Private = 4,
    Any = Public | Internal | Private,
    Default = Public,
}

/// <summary>
/// Generates schema root information.
/// </summary>
public class SchemaRootInfoGenerator(SchemaRootInfoGenerator.Options? options = null)
{
    private readonly Options _options = options ?? Options.Default;

    public record TypeOptions(
        AllowedTypeDeclarations AllowedTypeDeclarations = AllowedTypeDeclarations.Any,
        AllowedAccessibilities AllowedAccessibilities = AllowedAccessibilities.Default);

    public record MemberOptions(
        AllowedAccessibilities AllowedAccessibilities = AllowedAccessibilities.Default);

    public record Options(
        TypeOptions TypeOptions,
        MemberOptions MemberOptions)
    {
        public static Options Default { get; } = new(
            new TypeOptions(),
            new MemberOptions());

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
            schemaRootInfos,
            cancellationToken);

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
        List<SchemaRootInfo> schemaRootInfos,
        CancellationToken ct)
        : CSharpSyntaxWalker
    {
        private readonly SchemaMember.Object.SyntaxVisitor _objectVisitor = new(options, compilation);

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (!options.TypeOptions.AllowedTypeDeclarations.HasFlag(AllowedTypeDeclarations.Class))
                return;

            this.ProcessTypeDeclaration(node);
        }

        public override void VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (!options.TypeOptions.AllowedTypeDeclarations.HasFlag(AllowedTypeDeclarations.Struct))
                return;

            this.ProcessTypeDeclaration(node);
        }

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

        private void ProcessTypeDeclaration(TypeDeclarationSyntax node)
        {
            SemanticModel semanticModel = compilation.GetSemanticModel(node.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(node, ct) is not INamedTypeSymbol symbol)
                return;

            if (!symbol.ShouldProcessAccessibility(options))
                return;

            if (symbol.GetAttributeData<SchemaRootAttribute>() is not AttributeData attributeData)
                return;

            if (node.Accept(_objectVisitor) is not SchemaMember.Object rootType)
                return;

            schemaRootInfos.Add(GetSchemaRootInfo(rootType, attributeData));
        }

        private static SchemaRootInfo GetSchemaRootInfo(SchemaMember.Object rootType, AttributeData attributeData)
        {
            string? filename = attributeData.GetNamedArgument<string>(nameof(SchemaRootAttribute.Filename));
            string? id = attributeData.GetNamedArgument<string>(nameof(SchemaRootAttribute.Id));
            string? commonNamespace = attributeData.GetNamedArgument<string>(nameof(SchemaRootAttribute.CommonNamespace));
            return new(rootType, filename, id, commonNamespace);
        }
    }
}
