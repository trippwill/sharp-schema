using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using SharpSchema.Generator.Tests.TestUtilities;
using Xunit;

namespace SharpSchema.Generator.Tests.DeclaredTypeSyntaxVisitorTests;

public class TestDataFixture
{
    private readonly SyntaxTree _syntaxTree;
    private readonly CSharpCompilation _compilation;

    public TestDataFixture()
    {
        string pathToTestData = PathHelper.GetRepoPath(
                "test",
                "SharpSchema.Generator.Tests",
                "DeclaredTypeSyntaxVisitorTests",
                "TestData.cs");

        // Create an array of syntax tree from all cs files in src/SharpSchema.Annotations/
        string[] annotationFiles = Directory.GetFiles(
            PathHelper.GetRepoPath("src", "SharpSchema.Annotations"), "*.cs", SearchOption.AllDirectories);

        List<SyntaxTree> annotationSyntaxTrees = [.. annotationFiles.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file)))];

        _syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(pathToTestData));
        _compilation = CSharpCompilation.Create("TestDataCompilation")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees([.. annotationSyntaxTrees, _syntaxTree]);
    }

    public DeclaredTypeSyntaxVisitor GetVisitor() => new(_compilation, GeneratorOptions.Default);

    public JsonSchemaBuilder GetJsonSchemaBuilder(DeclaredTypeSyntaxVisitor visitor, [CallerMemberName] string? testName = null)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        ArgumentNullException.ThrowIfNull(testName);

        TypeDeclarationSyntax typeDeclaration = _syntaxTree
            .GetRoot()
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Where(type => type.Identifier.Text == testName)
            .First();

        JsonSchemaBuilder? builder = visitor.Visit(typeDeclaration);
        Assert.NotNull(builder);
        return builder;
    }
}
