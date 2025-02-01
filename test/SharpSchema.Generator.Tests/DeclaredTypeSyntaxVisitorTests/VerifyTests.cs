using System;
using System.Runtime.CompilerServices;
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

    [Fact]
    public async Task Class_WithDocComments() => await RunTestAsync();

    [Fact]
    public async Task Class_WithValueTypes() => await RunTestAsync();

    [Fact]
    public async Task Struct_WithNullableValueTypes() => await RunTestAsync();

    [Fact]
    public async Task Record_WithReferenceTypeProperties() => await RunTestAsync();

    [Fact]
    public async Task Record_WithReferenceTypeParameters() => await RunTestAsync();

    [Fact]
    public async Task Record_WithValueTypeParameters() => await RunTestAsync();

    [Fact]
    public async Task Class_WithIEnumerableProperty() => await RunTestAsync();

    [Fact]
    public async Task Class_WithDictionaryOfValueTypes() => await RunTestAsync();

    [Fact]
    public async Task Class_WithDictionaryOfReferenceTypes() => await RunTestAsync();

    [Fact(Skip = "Not Working")]
    public async Task Class_WithSchemaOverride() => await RunTestAsync();

    [Fact(Skip = "Not Working")]
    public async Task Record_WithSchemaOverride() => await RunTestAsync();

    [Fact(Skip = "Not Working")]
    public async Task Class_WithTypeSchemaOverride() => await RunTestAsync();

    private async Task RunTestAsync([CallerMemberName] string? testName = null)
    {
        DeclaredTypeSyntaxVisitor visitor = _fixture.GetVisitor();
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, testName);
        _output.WriteSeparator();
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        await Verifier.Verify(schemaString);
    }
}
