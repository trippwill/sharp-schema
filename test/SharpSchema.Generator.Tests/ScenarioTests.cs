using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Tests.Utilities;

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
        });
    }

    public VerifySettings VerifySettings { get; }

    [Fact]
    public async Task VerifyAllFeaturesSchemaRootInfo()
    {
        using var workspace = MSBuildWorkspace.Create();
        string projectPath = PathHelper.GetProjectPath("test/generator-scenarios/AllFeatures/AllFeatures.csproj");
        Project project = await workspace.OpenProjectAsync(projectPath);

        var generator = new SchemaRootInfoGenerator();
        List<SchemaRootInfo> schemaRootInfos = new();

        await foreach (SchemaRootInfo value in generator.FindSchemaRootTypesAsync(project, CancellationToken.None))
        {
            schemaRootInfos.Add(value);
        }

        SchemaRootInfo schemaRootInfo = Assert.Single(schemaRootInfos);
        await Verify(schemaRootInfo, this.VerifySettings);
    }
}

