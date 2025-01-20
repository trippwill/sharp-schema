using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Tests.Utilities;
using SharpSchema.Generator.Tests.VerifyConverters;

namespace SharpSchema.Generator.Tests;

internal class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initializer()
    {
        MSBuildLocator.RegisterDefaults();
    }
}

public class ScenarioTests
{
    public ScenarioTests()
    {
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
    public async Task VerifyAllFeaturesSchemaRootInfo(SchemaRootInfoGenerator.Options options)
    {
        using var workspace = MSBuildWorkspace.Create();
        string projectPath = PathHelper.GetProjectPath("test/generator-scenarios/AllFeatures/AllFeatures.csproj");
        Project project = await workspace.OpenProjectAsync(projectPath);

        var generator = new SchemaRootInfoGenerator(options);
        IReadOnlyCollection<SchemaRootInfo> schemaRootInfos = await generator.FindRootsAsync(
            project,
            CancellationToken.None);

        SchemaRootInfo schemaRootInfo = Assert.Single(schemaRootInfos);
        await Verify(schemaRootInfo, this.VerifySettings).UseParameters(options);
    }

    public static TheoryData<SchemaRootInfoGenerator.Options> VerifyAllFeaturesSchemaRootInfoOptions()
    {
        return new()
        {
            { SchemaRootInfoGenerator.Options.Default },

            { new SchemaRootInfoGenerator.Options(
                new SchemaRootInfoGenerator.TypeOptions(
                    AllowedTypeDeclarations.Class,
                    AllowedAccessibilities.Public),
                new SchemaRootInfoGenerator.MemberOptions(
                    AllowedAccessibilities.Public)) },
        };
    }

}

