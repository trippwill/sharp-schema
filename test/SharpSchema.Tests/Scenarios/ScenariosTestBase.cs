using System.Text.Encodings.Web;
using System.Text.Json;
using Json.More;
using Json.Schema;
using SharpSchema;
using Xunit;
using Xunit.Abstractions;

namespace Scenarios;

public abstract class ScenarioTestBase<TRoot>(ITestOutputHelper outputHelper)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    [Fact]
    public void Scenario_OutputsExpectedSchema()
    {
        RootTypeContext typeContext = RootTypeContext.FromType<TRoot>() with
        {
            Id = "https://libanvl/test/scenario",
            CommonNamespace = this.CommonNamespace,
        };

        TypeConverter.Options options = new()
        {
            ParseDocComments = true,
            MaxDepth = 10,
        };

        JsonSchema schema = new TypeConverter(options).Convert(typeContext);
        string schemaString = JsonSerializer.Serialize(
            schema.ToJsonDocument().RootElement,
            JsonSerializerOptions);

        outputHelper.WriteLine(schemaString);

        Assert.Equal(this.ExpectedSchema, schemaString);
    }

    protected abstract string ExpectedSchema { get; }

    protected abstract string CommonNamespace { get; }
}
