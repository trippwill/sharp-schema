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

    ////[Theory]
    //[MemberData(nameof(VerifyOptions))]
    //public async Task Verify_SchemaTree(AllowedAccessibilities typeOptions, AllowedAccessibilities memberOptions)
    //{
    //    SchemaTreeGenerator.Options options = new(typeOptions, memberOptions);
    //    SchemaTree schemaTree = await GetSchemaTreeAsync("test.schema.json", options);
    //    await Verify(schemaTree, this.VerifySettings).UseParameters(options);
    //}

    //[Theory]
    //[MemberData(nameof(VerifyOptions))]
    //public async Task Verify_TreeHasStableSchemaHash(AllowedAccessibilities typeOptions, AllowedAccessibilities memberOptions)
    //{
    //    SchemaTreeGenerator.Options options = new(typeOptions, memberOptions);
    //    SchemaTree schemaTree = await GetSchemaTreeAsync("test.schema.json", options);
    //    await Verify(schemaTree.GetSchemaHash()).UseParameters(options);
    //}

    //[Theory]
    //[MemberData(nameof(VerifyOptions))]
    //public async Task Verify_JsonSchema(AllowedAccessibilities typeOptions, AllowedAccessibilities memberOptions)
    //{
    //    SchemaTreeGenerator.Options options = new(typeOptions, memberOptions);
    //    SchemaTree schemaTree = await GetSchemaTreeAsync("test.schema.json", options);

    //    var jsonSchemaGenerator = new JsonSchemaGenerator();
    //    (JsonSchema jsonSchema, string? _) = jsonSchemaGenerator.Generate(schemaTree);

    //    string json = jsonSchema.SerializeToJson();
    //    _outputHelper.WriteLine(json);

    //    await Verify(json).UseParameters(options);
    //}

    //[Fact]
    //[MemberData(nameof(VerifyOptions))]
    //public async Task Section_Verify_JsonSchema(/*AllowedAccessibilities typeOptions, AllowedAccessibilities memberOptions*/)
    //{
    //    // SchemaTreeGenerator.Options options = new(typeOptions, memberOptions);
    //    SchemaTree schemaTree = await GetSchemaTreeAsync("section.schema.json" /*, options */);

    //    var jsonSchemaGenerator = new JsonSchemaGenerator();
    //    (JsonSchema jsonSchema, string? _) = jsonSchemaGenerator.Generate(schemaTree);

    //    string json = jsonSchema.SerializeToJson();
    //    _outputHelper.WriteLine(json);

    //    //await Verify(json).UseParameters(options);
    //}

    //[Fact]
    //public async Task DifferentTrees_HaveDifferentSchemaHashes()
    //{
    //    SchemaTree schemaTree1 = await GetSchemaTreeAsync(
    //        "test.schema.json",
    //        new(
    //            AllowedAccessibilities.Public,
    //            AllowedAccessibilities.Public));
    //    SchemaTree schemaTree2 = await GetSchemaTreeAsync(
    //        "test.schema.json",
    //        new(
    //            AllowedAccessibilities.Public | AllowedAccessibilities.Internal,
    //            AllowedAccessibilities.Public | AllowedAccessibilities.Internal));

    //    long hash1 = schemaTree1.GetSchemaHash();
    //    long hash2 = schemaTree2.GetSchemaHash();
    //    Assert.NotEqual(hash1, hash2);
    //}

    //[Fact]
    //public async Task PrintSchemas()
    //{
    //    IReadOnlyCollection<JsonSchema> schemaRootInfos = await GetSchemaTreeAsync();
    //    foreach (JsonSchema schemaRootInfo in schemaRootInfos)
    //    {
    //        string json = schemaRootInfo.SerializeToJson();
    //        _outputHelper.WriteLine(json);
    //        _outputHelper.WriteLine(string.Empty);
    //        _outputHelper.WriteLine(string.Empty);
    //    }
    //}

    //private async Task<IReadOnlyCollection<JsonSchema>> GetSchemaTreeAsync(SchemaTreeGenerator.Options? options = null)
    //{
    //    var generator = new SchemaTreeGenerator(options);
    //    IReadOnlyCollection<JsonSchema> schemaRootInfos = await generator
    //        .FindRootsAsync(
    //            _fixture.Project!,
    //            CancellationToken.None);

    //    return schemaRootInfos;
    //}

    public static TheoryData<AllowedAccessibilities, AllowedAccessibilities> VerifyOptions()
    {
        return new()
        {
            { AllowedAccessibilities.Default, AllowedAccessibilities.Default},
            { AllowedAccessibilities.Any, AllowedAccessibilities.Any }
        };
    }
}
