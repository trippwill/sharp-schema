﻿using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.IO;
using SharpSchema.Annotations;
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
            Path.Combine(
                "test",
                "SharpSchema.Generator.Tests",
                "DeclaredTypeSyntaxVisitorTests",
                "TestData.cs"));

        _syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(pathToTestData));
        _compilation = CSharpCompilation.Create("TestDataCompilation")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddReferences(MetadataReference.CreateFromFile(typeof(SchemaOverrideAttribute).Assembly.Location))
            .AddSyntaxTrees(_syntaxTree);
    }

    public DeclaredTypeSyntaxVisitor GetVisitor() => new(_compilation);

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
