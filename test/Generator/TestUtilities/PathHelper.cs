using System;
using System.IO;

namespace SharpSchema.Test.Generator.TestUtilities;

public static class PathHelper
{
    public static string GetRepoPath(params string[] relativePath)
    {
        string basePath = AppContext.BaseDirectory;
        string repoRoot = FindRepoRoot(basePath);
        string projectPath = Path.GetFullPath(Path.Combine(repoRoot, Path.Combine(relativePath)));
        return projectPath;
    }

    private static string FindRepoRoot(string currentPath)
    {
        while (!string.IsNullOrEmpty(currentPath))
        {
            if (Directory.Exists(Path.Combine(currentPath, ".git")))
                return currentPath;

            DirectoryInfo? parentDirectory = Directory.GetParent(currentPath);
            if (parentDirectory is null)
                break;

            currentPath = parentDirectory.FullName;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
