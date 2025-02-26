using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using Xunit;
using SharpSchema.Generator;
using SharpSchema.Test.Generator.TestUtilities;
using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace SharpSchema.Test.Generator.RootSyntaxVisitorTests;

public class TestDataFixture
{
    private readonly SyntaxTree _syntaxTree;
    private readonly CSharpCompilation _compilation;

    public TestDataFixture()
    {
        string pathToTestData = PathHelper.GetRepoPath(
                "test",
                "Generator",
                "RootSyntaxVisitorTests",
                "TestData.cs");

        // Create an array of syntax tree from all cs files in src/SharpSchema.Annotations/
        string[] annotationFiles = Directory.GetFiles(
            PathHelper.GetRepoPath("src", "SharpSchema.Annotations"), "*.cs", SearchOption.AllDirectories);

        List<SyntaxTree> annotationSyntaxTrees = [.. annotationFiles.Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file)))];

        _syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(pathToTestData));
        _compilation = CSharpCompilation.Create("TestDataCompilation")
            .AddReferences(
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ImmutableArray).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JsonIgnoreAttribute).Assembly.Location))
            .AddSyntaxTrees([.. annotationSyntaxTrees, _syntaxTree]);
    }

    public RootSyntaxVisitor GetVisitor(GeneratorOptions options) => new(_compilation, options);

    public JsonSchemaBuilder GetJsonSchemaBuilder(RootSyntaxVisitor visitor, [CallerMemberName] string? testName = null)
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
