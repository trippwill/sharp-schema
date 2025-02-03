using System;
using System.Threading.Tasks;
using Json.Schema;
using SharpSchema.Generator.Tests.TestUtilities;
using SharpSchema.Generator.Utilities;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace SharpSchema.Generator.Tests.DeclaredTypeSyntaxVisitorTests;

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
    [InlineData(nameof(Class_WithIEnumerableProperty))]
    [InlineData(nameof(Class_WithDictionaryOfValueTypes))]
    [InlineData(nameof(Class_WithDictionaryOfReferenceTypes))]
    [InlineData(nameof(Class_WithDocComments))]
    [InlineData(nameof(Class_WithValueTypes))]
    [InlineData(nameof(Class_WithSchemaOverride))]
    [InlineData(nameof(Class_WithTypeSchemaOverride))]
    [InlineData(nameof(Class_WithBadTypeSchemaOverride))]
    [InlineData(nameof(Class_WithUnsupportedDictionaryKey))]
    [InlineData(nameof(Class_WithIgnoredProperty))]
    [InlineData(nameof(Class_WithInternalProperties))]
    public Task Verify_TestData(string testName)
    {
        DeclaredTypeSyntaxVisitor visitor = _fixture.GetVisitor();
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, testName);
        _output.WriteSeparator();
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        return Verifier.Verify(schemaString).UseParameters(testName);
    }
}
