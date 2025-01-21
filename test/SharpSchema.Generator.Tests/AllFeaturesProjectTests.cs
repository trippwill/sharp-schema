using System.Runtime.CompilerServices;
using Json.Schema;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Tests.Utilities;
using SharpSchema.Generator.Tests.VerifyConverters;
using Xunit.Abstractions;

namespace SharpSchema.Generator.Tests;

internal class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initializer()
    {
        MSBuildLocator.RegisterDefaults();
    }
}

public class AllFeaturesProjectTests
{
    private readonly ITestOutputHelper _outputHelper;

    public AllFeaturesProjectTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;

        VerifySettings = new VerifySettings();
        VerifySettings.AddExtraSettings(settings =>
        {
            settings.Converters.Add(new ISymbolJsonConverter());
            settings.Converters.Add(new EqualsValueClauseSyntaxConverter());
        });
    }

    public VerifySettings VerifySettings { get; }

    [Theory]
    [MemberData(nameof(VerifyAllFeaturesSchemaRootInfoOptions))]
    public async Task AllFeaturesProject_SchemaTree(SchemaTreeGenerator.Options options)
    {
        SchemaTree schemaTree = await GetSchemaTreeAsync(options);
        await Verify(schemaTree, this.VerifySettings).UseParameters(options);
    }

    [Fact]
    public async Task AllFeaturesProject_JsonSchema()
    {
        SchemaTree schemaTree = await GetSchemaTreeAsync();

        var jsonSchemaGenerator = new JsonSchemaGenerator();
        (JsonSchema jsonSchema, string? _) = jsonSchemaGenerator.Generate(schemaTree);

        string json = jsonSchema.SerializeToJson();
        _outputHelper.WriteLine(json);

        await Verify(json);
    }

    private static async Task<SchemaTree> GetSchemaTreeAsync(SchemaTreeGenerator.Options? options = null)
    {
        Project project = await WorkspaceHelper.LoadProjectAsync(
             PathHelper.GetRepoPath("test/generator-scenarios/AllFeatures/AllFeatures.csproj"));

        var generator = new SchemaTreeGenerator(options);
        IReadOnlyCollection<SchemaTree> schemaRootInfos = await generator.FindRootsAsync(
            project,
            CancellationToken.None);

        return Assert.Single(schemaRootInfos);
    }

    public static TheoryData<SchemaTreeGenerator.Options> VerifyAllFeaturesSchemaRootInfoOptions()
    {
        return new()
        {
            { SchemaTreeGenerator.Options.Default },

            //{ new SchemaRootInfoGenerator.Options(
            //    new SchemaRootInfoGenerator.TypeOptions(
            //        AllowedTypeDeclarations.Class,
            //        AllowedAccessibilities.Public),
            //    new SchemaRootInfoGenerator.MemberOptions(
            //        AllowedAccessibilities.Public)) },
        };
    }

}
