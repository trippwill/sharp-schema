using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using SharpSchema.Generator.Tests.Utilities;

namespace SharpSchema.Generator.Tests;

public class AllFeaturesProjectFixture : IAsyncLifetime
{
    public Project? Project { get; private set; }

    public async Task InitializeAsync()
    {
        MSBuildLocator.RegisterDefaults();
        using var workspace = MSBuildWorkspace.Create();
        Project = await WorkspaceHelper.LoadProjectAsync(
            PathHelper.GetRepoPath("test/generator-scenarios/AllFeatures/AllFeatures.csproj"));
    }

    public Task DisposeAsync()
    {
        // Cleanup if necessary
        return Task.CompletedTask;
    }
}
