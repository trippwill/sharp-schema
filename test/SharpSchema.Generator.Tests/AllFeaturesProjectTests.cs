using Json.Schema;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Tests.VerifyConverters;
using SharpSchema.Generator.Utilities;
using Xunit.Abstractions;

namespace SharpSchema.Generator.Tests;

public class AllFeaturesProjectTests : IClassFixture<AllFeaturesProjectFixture>
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly AllFeaturesProjectFixture _fixture;

    public AllFeaturesProjectTests(ITestOutputHelper outputHelper, AllFeaturesProjectFixture fixture)
    {
        _outputHelper = outputHelper;
        _fixture = fixture;

        VerifySettings = new VerifySettings();
        VerifySettings.AddExtraSettings(settings =>
        {
            settings.Converters.Add(new ISymbolJsonConverter());
            settings.Converters.Add(new EqualsValueClauseSyntaxConverter());
        });
    }

    public VerifySettings VerifySettings { get; }

    [Theory]
    [MemberData(nameof(VerifyOptions))]
    public async Task Verify_SchemaTree(AllowedAccessibilities typeOptions, AllowedAccessibilities memberOptions)
    {
        SchemaTreeGenerator.Options options = new(typeOptions, memberOptions);
        SchemaTree schemaTree = await GetSchemaTreeAsync(options);
        await Verify(schemaTree, this.VerifySettings).UseParameters(options);
    }

    [Theory]
    [MemberData(nameof(VerifyOptions))]
    public async Task Verify_TreeHasStableSchemaHash(AllowedAccessibilities typeOptions, AllowedAccessibilities memberOptions)
    {
        SchemaTreeGenerator.Options options = new(typeOptions, memberOptions);
        SchemaTree schemaTree = await GetSchemaTreeAsync(options);
        await Verify(schemaTree.GetSchemaHash()).UseParameters(options);
    }

    [Fact]
    public async Task Verify_JsonSchema()
    {
        SchemaTree schemaTree = await GetSchemaTreeAsync();

        var jsonSchemaGenerator = new JsonSchemaGenerator();
        (JsonSchema jsonSchema, string? _) = jsonSchemaGenerator.Generate(schemaTree);

        string json = jsonSchema.SerializeToJson();
        _outputHelper.WriteLine(json);

        await Verify(json);
    }

    private async Task<SchemaTree> GetSchemaTreeAsync(SchemaTreeGenerator.Options? options = null)
    {
        var generator = new SchemaTreeGenerator(options);
        IReadOnlyCollection<SchemaTree> schemaRootInfos = await generator
            .FindRootsAsync(
                _fixture.Project!,
                CancellationToken.None);

        return schemaRootInfos.Single();
    }

    public static TheoryData<AllowedAccessibilities, AllowedAccessibilities> VerifyOptions()
    {
        return new()
        {
            { AllowedAccessibilities.Default, AllowedAccessibilities.Default},
        };
    }
}
