using System.Diagnostics;
using System.Runtime.CompilerServices;
using Json.Schema;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SharpSchema.Generator.Tests.Utilities;
using SharpSchema.Generator.Utilities;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SharpSchema.Generator.Tests;

public class DeclaredTypeSyntaxVisitorTests : IDisposable
{
    private readonly ITestOutputHelper _output;

    public DeclaredTypeSyntaxVisitorTests(ITestOutputHelper outputHelper)
    {
        Trace.Listeners.Add(outputHelper.TraceListener());
        _output = outputHelper;
    }

    public void Dispose() => Trace.Listeners.Clear();

    [Fact]
    public void SingleClass_WithDocComments()
    {
        string code = """
            namespace SingleClassWithDocComments;

            public class Person
            {
                /// <jsonschema>
                ///     <title>The name of the person.</title>
                /// </jsonschema>
                public string Name { get; set; }

                /// <jsonschema>
                ///     <description>The age of the person.</description>
                /// </jsonschema>
                public int Age { get; set; }
            }
            """;

        JsonSchemaBuilder builder = GetSchemaBuilderFromVisitor(code);
        _output.WriteSeparator();
        _output.WriteLine(builder.Build().SerializeToJson());
    }

    [Fact]
    public void SingleClass_WithValueTypes()
    {
        string code = """
            namespace SingleClassWithValueTypes;

            public class Person
            {
                public string Name { get; set; }
                public int Age { get; set; }
            }
            """;

        JsonSchemaBuilder builder = GetSchemaBuilderFromVisitor(code);
        _output.WriteSeparator();
        _output.WriteLine(builder.Build().SerializeToJson());
    }

    [Fact]
    public void SingleStruct_WithNullableValueTypes()
    {
        string code = """
            namespace SingleStructWithNullableValueTypes;

            public struct Person
            {
                public string? Name { get; set; }
                public int? Age { get; set; }
            }
            """;

        JsonSchemaBuilder builder = GetSchemaBuilderFromVisitor(code);
        _output.WriteSeparator();
        _output.WriteLine(builder.Build().SerializeToJson());
    }

    [Fact]
    public void Record_WithReferenceTypesProperties()
    {
        string code = """
            namespace Record_WithReferenceTypesProperties;

            public record Person
            {
                public string Name { get; set; }
                public int Age { get; set; }
                public Address Address { get; set; }
            }

            public record Address
            {
                public string Street { get; set; }
                public string City { get; set; }
            }
            """;

        JsonSchemaBuilder builder = GetSchemaBuilderFromVisitor(code);
        _output.WriteSeparator();
        _output.WriteLine(builder.Build().SerializeToJson());
    }

    [Fact]
    public void SingleRecord_WithValueTypeParameters()
    {
        string code = """
            namespace SingleRecordWithValueTypeParameters;
            public record Person(string Name, int Age);
            """;

        JsonSchemaBuilder builder = GetSchemaBuilderFromVisitor(code);
        _output.WriteSeparator();
        _output.WriteLine(builder.Build().SerializeToJson());
    }

    [Fact]
    public void SingleClass_WithIEnumerableProperty()
    {
        string code = """
            using System.Collections.Generic;

            namespace ClassWithIEnumerableProperty;

            public class Container
            {
                public List<int> Numbers { get; set; }
            }
            """;

        JsonSchemaBuilder builder = GetSchemaBuilderFromVisitor(code);
        _output.WriteSeparator();
        _output.WriteLine(builder.Build().SerializeToJson());
    }

    [Fact]
    public void SingeClass_WithDictionaryOfValueTypes()
    {
        string code = """
            using System.Collections.Generic;

            namespace ClassWithDictionaryProperty;

            public class Container
            {
                public Dictionary<string, int> Data { get; set; }
            }
            """;

        JsonSchemaBuilder builder = GetSchemaBuilderFromVisitor(code);
        _output.WriteSeparator();
        _output.WriteLine(builder.Build().SerializeToJson());
    }

    [Fact]
    public void Class_WithDictionaryOfReferenceTypes()
    {
        string code = """
            using System.Collections.Generic;
            namespace ClassWithDictionaryProperty;

            public class Container
            {
                public Dictionary<string, Person> Data { get; set; }
            }

            public class Person
            {
                public string Name { get; set; }
                public int Age { get; set; } = 18;
            }
            """;

        JsonSchemaBuilder builder = GetSchemaBuilderFromVisitor(code);
        _output.WriteSeparator();
        _output.WriteLine(builder.Build().SerializeToJson());
    }

    private static JsonSchemaBuilder GetSchemaBuilderFromVisitor(string code, [CallerMemberName] string? testName = null)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(code, path: testName ?? throw new InvalidOperationException());
        CSharpCompilation compilation = CSharpCompilation.Create(
            testName ?? throw new InvalidOperationException(),
            [tree],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Dictionary<,>).Assembly.Location),
            ]);

        DeclaredTypeSyntaxVisitor visitor = new(compilation);

        TypeDeclarationSyntax typeDeclaration = tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().First();
        JsonSchemaBuilder? builder = visitor.Visit(typeDeclaration);

        Assert.NotNull(builder);
        return builder;
    }
}
