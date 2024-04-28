// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Reflection;

namespace SharpSchema.Tool;

/// <summary>
/// Initializes a new instance of the <see cref="AssemblyLoader"/> class.
/// </summary>
/// <param name="console">The console instance used for error output.</param>
/// <param name="directoryRecursionDepth">The maximum recursion depth when searching for reference files in directories.</param>
/// <param name="referenceFiles">A collection of reference files.</param>
/// <param name="referenceDirectories">A collection of reference directories.</param>
internal class AssemblyLoader(IConsole console, int directoryRecursionDepth, FileInfo[] referenceFiles, DirectoryInfo[] referenceDirectories) : IDisposable
{
    private readonly MetadataLoadContext context = GetContext(console, directoryRecursionDepth, referenceFiles, referenceDirectories);

    /// <inheritdoc/>
    public void Dispose() => this.context.Dispose();

    /// <summary>
    /// Loads an assembly from the specified file path along with optional reference files and directories.
    /// </summary>
    /// <param name="assemblyFile">The file path of the assembly to load.</param>
    /// <returns>The loaded assembly.</returns>
    public Assembly LoadAssembly(FileInfo assemblyFile)
    {
        return this.context.LoadFromAssemblyPath(assemblyFile.FullName);
    }

    private static MetadataLoadContext GetContext(IConsole console, int directoryRecursionDepth, ReadOnlyMemory<FileInfo> referenceFiles, ReadOnlyMemory<DirectoryInfo> referenceDirectories)
    {
        Dictionary<string, string> assemblyNamePathMap = new(StringComparer.OrdinalIgnoreCase);

        foreach (FileInfo referenceFile in referenceFiles.Span)
        {
            if (!referenceFile.Exists)
            {
                console.Error.Write($"Assembly not found: {referenceFile.FullName}\n");
                continue;
            }

            if (assemblyNamePathMap.ContainsKey(referenceFile.Name))
            {
                continue;
            }

            assemblyNamePathMap[referenceFile.Name] = referenceFile.FullName;
        }

        foreach (DirectoryInfo referenceDirectory in referenceDirectories.Span)
        {
            if (!referenceDirectory.Exists)
            {
                console.Error.Write($"Directory not found: {referenceDirectory.FullName}\n");
                continue;
            }

            foreach (string path in Directory.EnumerateFiles(referenceDirectory.FullName, "*.dll", new EnumerationOptions
            {
                RecurseSubdirectories = true,
                MaxRecursionDepth = directoryRecursionDepth,
            }))
            {
                if (assemblyNamePathMap.ContainsKey(Path.GetFileName(path)))
                {
                    continue;
                }

                assemblyNamePathMap[Path.GetFileName(path)] = path;
            }
        }

        PathAssemblyResolver resolver = new(assemblyNamePathMap.Values);
        return new(resolver);
    }
}