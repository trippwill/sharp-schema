// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Reflection;

namespace SharpSchema.Tool;

/// <summary>
/// Represents a class responsible for loading assemblies.
/// </summary>
internal class AssemblyLoader(IConsole console) : IDisposable
{
    private MetadataLoadContext? context;

    /// <inheritdoc/>
    public void Dispose() => this.context?.Dispose();

    /// <summary>
    /// Loads an assembly from the specified file path along with optional reference files and directories.
    /// </summary>
    /// <param name="assemblyFile">The file path of the assembly to load.</param>
    /// <param name="referenceFiles">Optional reference files to include.</param>
    /// <param name="referenceDirectories">Optional reference directories to include.</param>
    /// <returns>The loaded assembly.</returns>
    public Assembly LoadAssembly(FileInfo assemblyFile, FileInfo[]? referenceFiles, DirectoryInfo[]? referenceDirectories)
    {
        Dictionary<string, string> assemblyPathMap = new();
        assemblyPathMap.Add(assemblyFile.Name, assemblyFile.FullName);

        foreach (FileInfo referenceFile in referenceFiles ?? [])
        {
            if (!referenceFile.Exists)
            {
                console.Error.Write($"Assembly not found: {referenceFile.FullName}\n");
                continue;
            }

            if (assemblyPathMap.ContainsKey(referenceFile.Name))
            {
                console.Error.Write($"Duplicate reference: {referenceFile.FullName}\n");
                continue;
            }

            assemblyPathMap.Add(referenceFile.Name, referenceFile.FullName);
        }

        foreach (DirectoryInfo referenceDirectory in referenceDirectories ?? [])
        {
            if (!referenceDirectory.Exists)
            {
                console.Error.Write($"Directory not found: {referenceDirectory.FullName}\n");
                continue;
            }

            foreach (string path in Directory.EnumerateFiles(referenceDirectory.FullName, "*.dll", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                MaxRecursionDepth = 2,
            }))
            {
                if (assemblyPathMap.ContainsKey(Path.GetFileName(path)))
                {
                    console.Error.Write($"Duplicate reference: {path}\n");
                    continue;
                }

                assemblyPathMap.Add(Path.GetFileName(path), path);
            }
        }

        PathAssemblyResolver resolver = new(assemblyPathMap.Values);
        this.context = new MetadataLoadContext(resolver);

        return this.context.LoadFromAssemblyPath(assemblyFile.FullName);
    }
}
