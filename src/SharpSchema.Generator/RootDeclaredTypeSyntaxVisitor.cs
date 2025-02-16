using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Utilities;
using Json.Schema;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;

/// <summary>
/// The options for the generator.
/// </summary>
/// <param name="Accessibilities">The accessibilities to consider.</param>
/// <param name="Traversal">The traversal options.</param>
/// <param name="DictionaryKeyMode">The mode for dictionary keys.</param>
public record GeneratorOptions(
    Accessibilities Accessibilities = Accessibilities.Public,
    Traversal Traversal = Traversal.SymbolOnly,
    DictionaryKeyMode DictionaryKeyMode = DictionaryKeyMode.Loose)
{
    /// <summary>
    /// Gets the default generator options.
    /// </summary>
    public static GeneratorOptions Default { get; } = new GeneratorOptions();
}

/// <summary>
/// Specifies the mode for dictionary keys.
/// </summary>
public enum DictionaryKeyMode
{
    /// <summary>
    /// Loose mode allows any type of dictionary key,
    /// adding a $comment to the schema for non-string keys.
    /// </summary>
    Loose = 1,

    /// <summary>
    /// Strict mode requires dictionary keys to be strings,
    /// returning an $unsupportedObject for non-string keys.
    /// </summary>
    Strict,

    /// <summary>
    /// Silent mode allows any type of dictionary key.
    /// </summary>
    Silent,

    /// <summary>
    /// Skip mode skips properties with Dictionary of non-string keys.
    /// </summary>
    Skip,
}

/// <summary>
/// Visits C# syntax nodes to generate JSON schema builders.
/// </summary>
public class RootDeclaredTypeSyntaxVisitor : CSharpSyntaxVisitor<Builder?>
{
    private readonly LeafDeclaredTypeSyntaxVisitor _cachingVisitor;
    private readonly GeneratorOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RootDeclaredTypeSyntaxVisitor"/> class.
    /// </summary>
    /// <param name="compilation">The compilation context.</param>
    /// <param name="options">The generator options.</param>
    public RootDeclaredTypeSyntaxVisitor(Compilation compilation, GeneratorOptions options)
    {
        _cachingVisitor = new(compilation, options);
        _options = options;
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
        return base.Visit(node);
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
