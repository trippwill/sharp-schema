// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using Json.More;
using Json.Schema;
using SharpMeta;
using static SharpSchema.Tool.GenerateCommandHandler;

namespace SharpSchema.Tool;

/// <summary>
/// Represents the command handler for the generate command.
/// </summary>
internal class GenerateCommandHandler(IConsole console, LoaderOptions loaderOptions, ConverterOptions converterOptions, WriterOptions writerOptions)
{
    /// <summary>
    /// Invokes the generate command.
    /// </summary>
    /// <param name="classNames">The names of the classes to generate schemas for.</param>
    /// <param name="assemblyFile">The assembly file to load.</param>
    /// <returns>The exit code of the command.</returns>
    public ExitCode Invoke(string[] classNames, FileInfo assemblyFile)
    {
        ExitCode exitCode = ExitCode.Success;
        try
        {
            using AssemblyLoadContext loader = new(
                console.Error.Write,
                loaderOptions.DirectoryRecursionDepth,
                [.. loaderOptions.ReferenceFiles, assemblyFile],
                [.. loaderOptions.ReferenceDirectories],
                includeExecutingCoreAssembly: false,
                includeExecutingRuntimeAssemblies: false);

            Assembly assembly = loader.LoadAssembly(assemblyFile);

            foreach (string className in classNames)
            {
                Type? rootType = assembly.GetType(className);

                if (rootType is null)
                {
                    console.Error.Write($"Class {className} not found in assembly.\n");
                    exitCode = ExitCode.ClassNotFoundError;
                    continue;
                }

                var rootTypeContext = RootTypeContext.FromType(rootType);

                console.Out.Write($"Generating schema for {rootTypeContext.Type.FullName}...\n");

                TypeConverter converter = new(new TypeConverter.Options
                {
                    IncludeInterfaces = converterOptions.IncludeInterfaces,
                    EnumAsUnderlyingType = converterOptions.EnumAsUnderlyingType,
                    MaxDepth = converterOptions.MaxDept,
                });

                JsonSchema schema = converter
                    .Convert(rootTypeContext)
                    .Build();

                string schemaString = JsonSerializer.Serialize(
                    schema.ToJsonDocument().RootElement,
                    new JsonSerializerOptions
                    {
                        WriteIndented = writerOptions.WriteIndented,
                        Encoder = writerOptions.StrictJsonEscaping
                            ? System.Text.Encodings.Web.JavaScriptEncoder.Default
                            : System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    });

                string fileName = rootTypeContext.Filename ?? $"{rootTypeContext.Type.Name}.schema.json";

                if (writerOptions.OutputDirectory is null)
                {
                    console.Out.Write(schemaString);
                }
                else
                {
                    FileInfo outputFile = new(Path.Combine(writerOptions.OutputDirectory.FullName, fileName));
                    if (outputFile.DirectoryName is string directoryName)
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    if (outputFile.Exists && !writerOptions.Overwrite)
                    {
                        console.Error.Write($"Output file {outputFile.FullName} already exists. Use --overwrite to overwrite.\n");
                        exitCode = ExitCode.OutputFileExists;
                        continue;
                    }

                    File.WriteAllText(outputFile.FullName, schemaString);
                    console.Out.Write($"Schema written to {outputFile.FullName}\n");
                }
            }
        }
        catch (FileLoadException flex)
        {
            console.Error.Write($"Error loading input assembly: {flex.Message}\n");
            exitCode = ExitCode.AssemblyLoadError;
        }
        catch (Exception ex)
        {
            console.Error.Write($"Error: {ex.Message}\n");
            exitCode = ExitCode.Error;
        }

        return exitCode;
    }

    /// <summary>
    /// Represents the options for the loader.
    /// </summary>
    public record struct LoaderOptions(int DirectoryRecursionDepth, FileInfo[] ReferenceFiles, DirectoryInfo[] ReferenceDirectories);

    /// <summary>
    /// Represents the options for the writer.
    /// </summary>
    public record struct WriterOptions(DirectoryInfo? OutputDirectory, bool WriteIndented, bool StrictJsonEscaping, bool Overwrite);

    /// <summary>
    /// Represents the options for the converter.
    /// </summary>
    public record struct ConverterOptions(bool IncludeInterfaces, bool EnumAsUnderlyingType, int MaxDept);
}