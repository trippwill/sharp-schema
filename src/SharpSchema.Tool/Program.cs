// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.Json;
using Json.More;
using Json.Schema;

namespace SharpSchema.Tool;

/// <summary>
/// Represents the exit codes for the application.
/// </summary>
internal enum ExitCode
{
    /// <summary>
    /// The operation was successful.
    /// </summary>
    Success = 0,

    /// <summary>
    /// An error occurred during the operation.
    /// </summary>
    Error = 1,

    /// <summary>
    /// Error loading the assembly.
    /// </summary>
    AssemblyLoadError = 2,

    /// <summary>
    /// The specified class was not found.
    /// </summary>
    ClassNotFoundError = 3,

    /// <summary>
    /// The output file already exists.
    /// </summary>
    OutputFileExists = 4,
}

/// <summary>
/// Program entry point.
/// </summary>
internal class Program
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// The entry point of the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The exit code of the application.</returns>
    public static async Task<int> Main(string[] args)
    {
        Option<FileInfo> assemblyOption = new(
            ["--assembly", "-a"],
            description: "The assembly containing the root class to generate a schema for.",
            parseArgument: result =>
            {
                FileInfo fileInfo = new(result.Tokens.Single().Value);
                if (!fileInfo.Exists)
                {
                    result.ErrorMessage = "File does not exist";
                }

                return fileInfo;
            })
        {
            IsRequired = true,
        };

        Option<string> classNameOption = new(
            ["--class-name", "-c"],
            description: "The name of the root class to generate a schema for.")
        {
            IsRequired = true,
        };

        Option<FileInfo?> outputOption = new(
            ["--output", "-o"],
            description: "The file to write the schema to. When not provided, outputs to the console.")
        {
            IsRequired = false,
        };

        Option<bool> overwriteOption = new(
            "--overwrite",
            description: "Overwrite the output file if it exists.",
            getDefaultValue: () => false);

        Option<FileInfo[]?> referenceOption = new(
            ["--reference", "-r"],
            description: "The assembly to reference.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrMore,
        };

        Option<DirectoryInfo[]?> referenceDirectoryOption = new(
            ["--reference-directory", "-d"],
            description: "The directory containing assemblies to reference.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrMore,
        };

        RootCommand rootCommand = new("Generates a JSON schema from a .NET class.")
        {
            assemblyOption,
            classNameOption,
            outputOption,
            overwriteOption,
            referenceOption,
            referenceDirectoryOption,
        };

        rootCommand.SetHandler(
            (assemblyInfo, className, outputInfo, overwrite, referenceInfos, referenceDirectoryInfos) =>
            {
                ExitCode exitCode = ExitCode.Success;
                using (AssemblyLoader loader = new(DefaultConsole.Instance))
                {
                    try
                    {
                        Assembly assembly = loader.LoadAssembly(assemblyInfo, referenceInfos, referenceDirectoryInfos);
                        Type? type = assembly.GetType(className);

                        if (type is null)
                        {
                            DefaultConsole.Instance.Error.Write($"Type {className} not found in input assembly.\n");
                            exitCode = ExitCode.ClassNotFoundError;
                            goto exit;
                        }

                        JsonSchema schema = type.ToJsonSchema().Build();

                        string schemaString = JsonSerializer.Serialize(
                            schema.ToJsonDocument().RootElement,
                            WriteOptions);

                        if (outputInfo is null)
                        {
                            DefaultConsole.Instance.Out.Write(schemaString);
                        }
                        else
                        {
                            if (outputInfo.Exists && !overwrite)
                            {
                                DefaultConsole.Instance.Error.Write($"Output file {outputInfo.FullName} already exists. Use --overwrite to overwrite.\n");
                                exitCode = ExitCode.OutputFileExists;
                                goto exit;
                            }

                            File.WriteAllText(outputInfo.FullName, schemaString);
                        }
                    }
                    catch (FileLoadException flex)
                    {
                        DefaultConsole.Instance.Error.Write($"Error loading input assembly: {flex.Message}\n");
                        exitCode = ExitCode.AssemblyLoadError;
                    }
                    catch (Exception ex)
                    {
                        DefaultConsole.Instance.Error.Write($"Error: {ex.Message}\n");
                        exitCode = ExitCode.Error;
                    }
                }

                exit:
                return Task.FromResult((int)exitCode);
            },
            assemblyOption,
            classNameOption,
            outputOption,
            overwriteOption,
            referenceOption,
            referenceDirectoryOption);

        return await rootCommand.InvokeAsync(args);
    }

    private class DefaultConsole : IConsole
    {
        private DefaultConsole()
        {
        }

        public static IConsole Instance { get; } = new DefaultConsole();

        public bool IsOutputRedirected => Console.IsOutputRedirected;

        public bool IsErrorRedirected => Console.IsErrorRedirected;

        public bool IsInputRedirected => Console.IsInputRedirected;

        public IStandardStreamWriter Out => StandardStreamWriter.Create(Console.Out);

        public IStandardStreamWriter Error => StandardStreamWriter.Create(Console.Error);
    }
}
