using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Utilities;
using Json.Schema;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;

/// <summary>
/// Visits C# syntax nodes to generate JSON schema builders.
/// </summary>
public class RootDeclaredTypeSyntaxVisitor : CSharpSyntaxVisitor<Builder?>
{
    private readonly LeafDeclaredTypeSyntaxVisitor _cachingVisitor;

    /// <summary>
    /// Initializes a new instance of the <see cref="RootDeclaredTypeSyntaxVisitor"/> class.
    /// </summary>
    /// <param name="compilation">The compilation context.</param>
    /// <param name="options">The generator options.</param>
    public RootDeclaredTypeSyntaxVisitor(Compilation compilation, GeneratorOptions options)
    {
        _cachingVisitor = new(compilation, options);
    }

    /// <inheritdoc />
    /// <summary>
    /// Visits a syntax node.
    /// </summary>
    /// <param name="node">The syntax node to visit.</param>
    /// <returns>A JSON schema builder or null.</returns>
    public override Builder? Visit(SyntaxNode? node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter($"[ROOT] {node.Kind()}");
        Builder? builder = base.Visit(node);

        Builder result = new Builder()
            .Schema("http://json-schema.org/draft-07/schema#");

        if (builder is not null)
        {
            result.ApplySchema(builder);
        }

        return result;
    }

    /// <inheritdoc />
    /// <summary>
    /// Visits a class declaration syntax node.
    /// </summary>
    /// <param name="node">The class declaration syntax node to visit.</param>
    /// <returns>A JSON schema builder or null.</returns>
    public override Builder? VisitClassDeclaration(ClassDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node);
    }

    /// <inheritdoc />
    /// <summary>
    /// Visits a struct declaration syntax node.
    /// </summary>
    /// <param name="node">The struct declaration syntax node to visit.</param>
    /// <returns>A JSON schema builder or null.</returns>
    public override Builder? VisitStructDeclaration(StructDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node);
    }

    /// <inheritdoc />
    /// <summary>
    /// Visits a record declaration syntax node.
    /// </summary>
    /// <param name="node">The record declaration syntax node to visit.</param>
    /// <returns>A JSON schema builder or null.</returns>
    public override Builder? VisitRecordDeclaration(RecordDeclarationSyntax node)
    {
        Throw.IfNullArgument(node);
        using var trace = Tracer.Enter(node.Identifier.Text);
        return this.VisitTypeDeclaration(node);
    }

    /// <summary>
    /// Visits a type declaration syntax node.
    /// </summary>
    /// <param name="node">The type declaration syntax node to visit.</param>
    /// <returns>A JSON schema builder or null.</returns>
    private Builder? VisitTypeDeclaration(TypeDeclarationSyntax node)
    {
        Builder builder = node.CreateTypeSchema(_cachingVisitor);

        if (_cachingVisitor.GetCachedSchemas() is IReadOnlyDictionary<string, JsonSchema> defs)
        {
            builder.Defs(defs);
        }

        return builder;
    }
}
