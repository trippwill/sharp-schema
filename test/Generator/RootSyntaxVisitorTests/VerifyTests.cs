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
        _output = outputHelper;
    }

    public void Dispose() => Tracer.Writer = null;

    internal static Task Verify(string schemaString, string testName, string parameter)
    {
        return Verifier.Verify(schemaString)
            .UseDirectory("Verifications")
            .UseFileName($"{testName}_{parameter}");
    }

    [Theory]
    [InlineData(nameof(Record_WithValueParameters))]
    [InlineData(nameof(Record_WithDefaultValueParameter))]
    [InlineData(nameof(Record_WithValueParametersAndProperty))]
    [InlineData(nameof(Record_WithValueParametersAndPropertyInitializer))]
    [InlineData(nameof(Record_WithDefaultValueParametersAndConstantProperty))]
    [InlineData(nameof(Record_WithDocComments))]
    [InlineData(nameof(Class_WithDocComments))]
    [InlineData(nameof(Class_WithArrayProperties))]
    [InlineData(nameof(GameHall))]
    public Task Verify_DefaultOptions(string testName)
    {
        Tracer.EnableTiming = true;

        RootSyntaxVisitor visitor = _fixture.GetVisitor(GeneratorOptions.Default);
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, testName);
        _output.WriteSeparator();
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        _output.WriteSeparator();

        //// Get type instance from test name
        //Type? type = Assembly.GetExecutingAssembly().GetType($"SharpSchema.Generator.TestData.{testName}");
        //if (type is not null)
        //{
        //    System.Text.Json.Nodes.JsonNode n = JsonSchemaExporter.GetJsonSchemaAsNode(JsonSerializerOptions.Default, type, JsonSchemaExporterOptions.Default);
        //    _output.WriteLine(n.ToString());
        //}

        return Verify(schemaString, "DefaultOptions", testName);
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
        
        return Verify(schemaString, "DictionaryKeyMode", dictionaryKeyMode.ToString());
    }

    [InlineData(AccessibilityMode.Public)]
    [InlineData(AccessibilityMode.Internal)]
    [InlineData(AccessibilityMode.Private)]
    [InlineData(AccessibilityMode.PublicInternal)]
    [Theory]
    public Task Verify_AccessibilityMode(AccessibilityMode accessibilities)
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
        
        return Verify(schemaString, "AccessibilityMode", accessibilities.ToString());
    }

    [InlineData(EnumMode.String)]
    [InlineData(EnumMode.UnderlyingType)]
    [Theory]
    public Task Verify_EnumMode(EnumMode enumHandling)
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

        return Verify(schemaString, "EnumMode", enumHandling.ToString());
    }

    [InlineData(TraversalMode.SymbolOnly)]
    [InlineData(TraversalMode.Bases)]
    [InlineData(TraversalMode.Interfaces)]
    [InlineData(TraversalMode.Full)]
    [Theory(Skip = "Traversal is not correctly implemented.")]
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
        
        return Verify(schemaString, "Traversal", traversal.ToString());
    }

    [InlineData(NumberMode.StrictDefs)]
    [InlineData(NumberMode.StrictInline)]
    [InlineData(NumberMode.JsonNative)]
    [Theory]
    public Task Verify_NumberMode(NumberMode numberMode)
    {
        GeneratorOptions options = new()
        {
            NumberMode = numberMode
        };

        RootSyntaxVisitor visitor = _fixture.GetVisitor(options);
        JsonSchemaBuilder builder = _fixture.GetJsonSchemaBuilder(visitor, nameof(Class_WithValueTypes));
        _output.WriteSeparator();
        
        string schemaString = builder.Build().SerializeToJson();
        _output.WriteLine(schemaString);
        
        return Verify(schemaString, "NumberMode", numberMode.ToString());
    }
}
