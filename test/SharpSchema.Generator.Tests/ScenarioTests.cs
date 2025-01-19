using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Tests.Utilities;

namespace SharpSchema.Generator.Tests
{
    public class ScenarioTests
    {
        public ScenarioTests()
        {
            MSBuildLocator.RegisterDefaults();
        }

        [Fact]
        public async Task VerifyAllFeaturesSchemaRootInfo()
        {
            using var workspace = MSBuildWorkspace.Create();
            string projectPath = PathHelper.GetProjectPath("test/generator-scenarios/AllFeatures/AllFeatures.csproj");
            Microsoft.CodeAnalysis.Project project = await workspace.OpenProjectAsync(projectPath);

            var generator = new SchemaRootInfoGenerator();
            List<SchemaRootInfo> schemaRootInfos = [];

            await foreach (SchemaRootInfo value in generator.FindSchemaRootTypesAsync(project, CancellationToken.None))
            {
                schemaRootInfos.Add(value);
            }

            SchemaRootInfo schemaRootInfo = Assert.Single(schemaRootInfos);
            await Verifier.Verify(schemaRootInfo);
        }
    }
}

