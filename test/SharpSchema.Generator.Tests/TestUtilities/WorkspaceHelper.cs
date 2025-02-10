using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace SharpSchema.Generator.Tests.TestUtilities;

public static class WorkspaceHelper
{
    public static async Task<Project> LoadProjectAsync(string projectPath)
    {
        using var workspace = MSBuildWorkspace.Create();
        return await workspace.OpenProjectAsync(projectPath);
    }
}
