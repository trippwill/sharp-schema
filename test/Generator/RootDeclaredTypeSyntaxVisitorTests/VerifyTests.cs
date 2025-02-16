using System;
using System.Threading.Tasks;
using Json.Schema;
using SharpSchema.Generator;
using SharpSchema.Generator.TestData;
using SharpSchema.Generator.Utilities;
using SharpSchema.Test.Generator.TestUtilities;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace SharpSchema.Test.Generator.RootDeclaredTypeSyntaxVisitorTests;

public class VerifyTests : IDisposable, IClassFixture<TestDataFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly TestDataFixture _fixture;

    public VerifyTests(TestDataFixture fixture, ITestOutputHelper outputHelper)
    {
        _fixture = fixture;
        Tracer.Writer = outputHelper.WriteLine;
        _output = outputHelper;
    }

    public void Dispose() => Tracer.Writer = null;

    [Theory]
    [InlineData(nameof(Struct_WithNullableValueTypes))]
    [InlineData(nameof(Record_WithReferenceTypeProperties))]
    [InlineData(nameof(Record_WithReferenceTypeParameters))]
    [InlineData(nameof(Record_WithValueTypeParameters))]
    [InlineData(nameof(Record_WithSchemaOverride))]
    [InlineData(nameof(Record_WithIgnoredParameter))]
    [InlineData(nameof(Record_WithParametersAndProperties))]
    [InlineData(nameof(Class_WithDictionaryProperties))]
    [InlineData(nameof(Class_WithDocComments))]
    [InlineData(nameof(Class_WithValueTypes))]
    [InlineData(nameof(Class_WithSchemaOverride))]
    [InlineData(nameof(Class_WithTypeSchemaOverride))]
    [InlineData(nameof(Class_WithUnsupportedDictionaryKey))]
    [InlineData(nameof(Class_WithIgnoredProperty))]
    [InlineData(nameof(Class_WithInternalProperties))]
    [InlineData(nameof(Class_WithArrayProperties))]
    [InlineData(nameof(Class_WithInvalidProperties))]
    public Task Verify_DefaultOptions(string testName)
    {
        RootDeclaredTypeSyntaxVisitor visitor = _fixture.GetVisitor(GeneratorOptions.Default);
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, testName);
        _output.WriteSeparator();
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        return Verifier.Verify(schemaString).UseParameters(testName);
    }

    [InlineData(DictionaryKeyMode.Loose)]
    [InlineData(DictionaryKeyMode.Strict)]
    [InlineData(DictionaryKeyMode.Silent)]
    [InlineData(DictionaryKeyMode.Skip)]
    [Theory]
    public Task Verify_DictionaryKeyMode(DictionaryKeyMode dictionaryKeyMode)
    {
        GeneratorOptions options = new()
        {
            DictionaryKeyMode = dictionaryKeyMode
        };

        RootDeclaredTypeSyntaxVisitor visitor = _fixture.GetVisitor(options);
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, nameof(Class_WithUnsupportedDictionaryKey));
        _output.WriteSeparator();
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        return Verifier.Verify(schemaString).UseParameters(dictionaryKeyMode);
    }

    [InlineData(Accessibilities.Public)]
    [InlineData(Accessibilities.Internal)]
    [InlineData(Accessibilities.Private)]
    [InlineData(Accessibilities.PublicInternal)]
    [Theory]
    public Task Verify_Accessibilities(Accessibilities accessibilities)
    {
        GeneratorOptions options = new()
        {
            Accessibilities = accessibilities
        };

        RootDeclaredTypeSyntaxVisitor visitor = _fixture.GetVisitor(options);
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, nameof(Class_WithInternalProperties));
        _output.WriteSeparator();
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        return Verifier.Verify(schemaString).UseParameters(accessibilities);
    }
}
