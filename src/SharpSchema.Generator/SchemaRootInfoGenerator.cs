using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator;

/// <summary>
/// Generates schema root information.
/// </summary>
public class SchemaRootInfoGenerator
{
    /// <summary>
    /// Finds schema root types in the given workspace.
    /// </summary>
    /// <param name="workspace">The workspace to search for schema root types.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable of <see cref="SchemaRootInfo"/>.</returns>
    [ExcludeFromCodeCoverage]
    public async Task<IReadOnlyCollection<SchemaRootInfo>> FindSchemaRootTypesAsync(
        Workspace workspace,
        CancellationToken cancellationToken)
    {
        Throw.IfNullArgument(workspace, nameof(workspace));

        var bag = new ConcurrentBag<SchemaRootInfo>();
        var tasks = workspace.CurrentSolution.Projects.Select(async project =>
        {
            await foreach (SchemaRootInfo rootInfo in this.FindSchemaRootTypesAsync(project, cancellationToken).WithCancellation(cancellationToken))
            {
                bag.Add(rootInfo);
            }
        });

        await Task.WhenAll(tasks);

        return bag.ToArray();
    }

    /// <summary>
    /// Finds schema root types in the given project.
    /// </summary>
    /// <param name="project">The project to search for schema root types.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>An async enumerable of <see cref="SchemaRootInfo"/>.</returns>
    public async IAsyncEnumerable<SchemaRootInfo> FindSchemaRootTypesAsync(
        Project project,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Throw.IfNullArgument(project, nameof(project));

        Compilation? compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation is null) yield break;

        SchemaRootInfo.SyntaxVisitor rootInfoVisitor = new(compilation, cancellationToken);
        CompilationUnitVisitor compilationUnitVisitor = new(rootInfoVisitor, cancellationToken);
        foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
        {
            SyntaxNode root = await syntaxTree.GetRootAsync(cancellationToken);
            foreach (SchemaRootInfo rootInfo in compilationUnitVisitor.Visit(root) ?? [])
            {
                yield return rootInfo;
            }
        }
    }

    private class CompilationUnitVisitor(SchemaRootInfo.SyntaxVisitor rootInfoVisitor, CancellationToken cancellationToken)
        : CSharpSyntaxVisitor<IEnumerable<SchemaRootInfo>>
    {
        private readonly SchemaRootInfo.SyntaxVisitor _rootInfoVisitor = rootInfoVisitor;
        private readonly CancellationToken _cancellationToken = cancellationToken;

        public override IEnumerable<SchemaRootInfo>? VisitCompilationUnit(CompilationUnitSyntax node)
        {
            foreach (MemberDeclarationSyntax member in node.Members)
            {
                foreach (SchemaRootInfo rootInfo in this.HandleMemberDeclaration(member) ?? [])
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    yield return rootInfo;
                }
            }
        }

        private IEnumerable<SchemaRootInfo> HandleMemberDeclaration(MemberDeclarationSyntax member)
        {
            switch (member)
            {
                case ClassDeclarationSyntax classDeclaration:
                    {
                        if (classDeclaration.Accept(_rootInfoVisitor) is SchemaRootInfo info)
                            yield return info;
                        break;
                    }
                case StructDeclarationSyntax structDeclaration:
                    {
                        if (structDeclaration.Accept(_rootInfoVisitor) is SchemaRootInfo info)
                            yield return info;
                        break;
                    }
                case NamespaceDeclarationSyntax namespaceDeclaration:
                    foreach (MemberDeclarationSyntax nestedMember in namespaceDeclaration.Members)
                    {
                        foreach (SchemaRootInfo rootInfo in this.HandleMemberDeclaration(nestedMember) ?? Array.Empty<SchemaRootInfo>())
                        {
                            _cancellationToken.ThrowIfCancellationRequested();
                            yield return rootInfo;
                        }
                    }
                    break;
                case FileScopedNamespaceDeclarationSyntax fileScopedNamespaceDeclaration:
                    foreach (MemberDeclarationSyntax nestedMember in fileScopedNamespaceDeclaration.Members)
                    {
                        foreach (SchemaRootInfo rootInfo in this.HandleMemberDeclaration(nestedMember) ?? Array.Empty<SchemaRootInfo>())
                        {
                            _cancellationToken.ThrowIfCancellationRequested();
                            yield return rootInfo;
                        }
                    }
                    break;
            }
        }
    }
}
