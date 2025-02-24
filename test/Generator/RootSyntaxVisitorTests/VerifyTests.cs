using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Schema;
using System.Threading.Tasks;
using Json.Schema;
using SharpSchema.Annotations;
using SharpSchema.Generator;
using SharpSchema.Generator.TestData;
using SharpSchema.Generator.Utilities;
using SharpSchema.Test.Generator.TestUtilities;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace SharpSchema.Test.Generator.RootSyntaxVisitorTests;

public class VerifyTests : IDisposable, IClassFixture<TestDataFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly TestDataFixture _fixture;

    public VerifyTests(TestDataFixture fixture, ITestOutputHelper outputHelper)
    {
        ArgumentNullException.ThrowIfNull(outputHelper);

        _fixture = fixture;
        Tracer.Writer = outputHelper.WriteLine;
        Tracer.EnableTiming = true;
        _output = outputHelper;
    }

    public void Dispose() => Tracer.Writer = null;

    [Theory]
    [InlineData(nameof(SimpleRecord))]
    [InlineData(nameof(Struct_WithNullableValueTypes))]
    [InlineData(nameof(Struct_WithAbstractProperties))]
    [InlineData(nameof(Record_WithReferenceTypeProperties))]
    [InlineData(nameof(Record_WithReferenceTypeParameters))]
    [InlineData(nameof(Record_WithValueTypeParameters))]
    [InlineData(nameof(Record_WithSchemaOverride))]
    [InlineData(nameof(Record_WithIgnoredParameter))]
    [InlineData(nameof(Record_WithParametersAndProperties))]
    [InlineData(nameof(Record_WithAbstractParameters))]
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
    [InlineData(nameof(Class_ExtendsAbstractClass))]
    [InlineData(nameof(Record_WithGenericAbstractProperty))]
    [InlineData(nameof(GameHall))]
    public Task Verify_DefaultOptions(string testName)
    {
        RootSyntaxVisitor visitor = _fixture.GetVisitor(GeneratorOptions.Default);
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, testName);
        _output.WriteSeparator();
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        _output.WriteSeparator();

        // Get type instance from test name
        Type? type = Assembly.GetExecutingAssembly().GetType($"SharpSchema.Generator.TestData.{testName}");
        if (type is not null)
        {
            System.Text.Json.Nodes.JsonNode n = JsonSchemaExporter.GetJsonSchemaAsNode(JsonSerializerOptions.Default, type, JsonSchemaExporterOptions.Default);
            _output.WriteLine(n.ToString());
        }

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

        RootSyntaxVisitor visitor = _fixture.GetVisitor(options);
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, nameof(Class_WithUnsupportedDictionaryKey));
        _output.WriteSeparator();
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        return Verifier.Verify(schemaString).UseParameters(dictionaryKeyMode);
    }

    [InlineData(AccessibilityMode.Public)]
    [InlineData(AccessibilityMode.Internal)]
    [InlineData(AccessibilityMode.Private)]
    [InlineData(AccessibilityMode.PublicInternal)]
    [Theory]
    public Task Verify_Accessibilities(AccessibilityMode accessibilities)
    {
        GeneratorOptions options = new()
        {
            AccessibilityMode = accessibilities
        };

        RootSyntaxVisitor visitor = _fixture.GetVisitor(options);
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, nameof(Class_WithInternalProperties));
        _output.WriteSeparator();
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        return Verifier.Verify(schemaString).UseParameters(accessibilities);
    }

    [InlineData(EnumMode.String)]
    [InlineData(EnumMode.UnderlyingType)]
    [Theory]
    public Task Verify_EnumHandling(EnumMode enumHandling)
    {
        GeneratorOptions options = new()
        {
            EnumMode = enumHandling
        };
        RootSyntaxVisitor visitor = _fixture.GetVisitor(options);
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, nameof(Record_WithAbstractParameters));
        _output.WriteSeparator();
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        return Verifier.Verify(schemaString).UseParameters(enumHandling);
    }

    [InlineData(TraversalMode.SymbolOnly)]
    [InlineData(TraversalMode.Bases)]
    [InlineData(TraversalMode.Interfaces)]
    [InlineData(TraversalMode.Full)]
    [Theory]
    public Task Verify_Traversal(TraversalMode traversal)
    {
        GeneratorOptions options = new()
        {
            TraversalMode = traversal
        };

        RootSyntaxVisitor visitor = _fixture.GetVisitor(options);
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, nameof(Class_ExtendsAbstractClass));
        _output.WriteSeparator();
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        return Verifier.Verify(schemaString).UseParameters(traversal);
    }
}
