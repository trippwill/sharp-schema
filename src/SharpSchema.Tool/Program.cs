// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.IO;
using System.Reflection;
using System.Text.Json;
using Json.More;
using Json.Schema;
using SharpSchema.Annotations;

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

        Option<string[]> classNameOption = new(
            ["--class", "-c"],
            description: "The root class to generate a schema for.")
        {
            IsRequired = true,
            Arity = ArgumentArity.OneOrMore,
            AllowMultipleArgumentsPerToken = true,
        };

        Option<DirectoryInfo?> outputOption = new(
            ["--output", "-o"],
            description: "The directory to write the generated schemas to. If no path is provided, outputs to the current directory. When this option is not provided, outputs to the console.",
            parseArgument: result =>
            {
                if (result.Tokens.Count != 1)
                {
                    return new DirectoryInfo(Environment.CurrentDirectory);
                }

                return new DirectoryInfo(result.Tokens.Single().Value);
            })
        {
            IsRequired = false,
        };

        Option<bool> overwriteOption = new(
            "--overwrite",
            description: "Overwrite the output file if it exists.",
            getDefaultValue: () => false);

        Option<FileInfo[]?> referenceOption = new(
            ["--reference", "-r"],
            description: "An assembly to reference.")
        {
            IsRequired = false,
            Arity = ArgumentArity.ZeroOrMore,
        };

        Option<DirectoryInfo[]?> referenceDirectoryOption = new(
            ["--reference-directory", "-d"],
            description: "A directory containing assemblies to reference.")
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
            (assemblyInfo, classNames, outputInfo, overwrite, referenceInfos, referenceDirectoryInfos) =>
            {
                ExitCode exitCode = ExitCode.Success;
                using (AssemblyLoader loader = new(DefaultConsole.Instance, directoryRecursionDepth: 3))
                {
                    try
                    {
                        Assembly assembly = loader.LoadAssembly(assemblyInfo, referenceInfos, referenceDirectoryInfos);

                        foreach (string className in classNames)
                        {
                            Type? rootType = assembly.GetType(className);

                            if (rootType is null)
                            {
                                DefaultConsole.Instance.Error.Write($"Class {className} not found in assembly.\n");
                                exitCode = ExitCode.ClassNotFoundError;
                                goto exit;
                            }

                            if (!rootType.TryGetCustomAttributeData(typeof(SchemaRootAttribute), out CustomAttributeData? cad))
                            {
                                DefaultConsole.Instance.Error.Write($"Class {className} must be decorated with SchemaRootAttribute.\n");
                                exitCode = ExitCode.Error;
                                goto exit;
                            }

                            RootTypeContext rootTypeContext = new(
                                rootType,
                                Filename: cad.GetNamedArgument<string>("Filename"),
                                Id: cad.GetNamedArgument<string>("Id"),
                                CommonNamespace: cad.GetNamedArgument<string>("CommonNamespace"));

                            DefaultConsole.Instance.Error.Write($"Generating schema for {rootTypeContext.Type.FullName}...\n");

                            JsonSchema schema = rootTypeContext.Type.ToJsonSchema(new ConverterContext()
                            {
                                DefaultNamespace = rootTypeContext.CommonNamespace,
                                Id = rootTypeContext.Id,
                            }).Build();

                            string schemaString = JsonSerializer.Serialize(
                                schema.ToJsonDocument().RootElement,
                                WriteOptions);

                            string fileName = rootTypeContext.Filename ?? $"{rootTypeContext.Type.Name}.schema.json";

                            if (outputInfo is null)
                            {
                                DefaultConsole.Instance.Out.Write(schemaString);
                            }
                            else
                            {
                                FileInfo outputFile = new(Path.Combine(outputInfo.FullName, fileName));
                                if (outputFile.DirectoryName is string directoryName)
                                {
                                    Directory.CreateDirectory(directoryName);
                                }

                                if (outputFile.Exists && !overwrite)
                                {
                                    DefaultConsole.Instance.Error.Write($"Output file {outputFile.FullName} already exists. Use --overwrite to overwrite.\n");
                                    exitCode = ExitCode.OutputFileExists;
                                    goto exit;
                                }

                                File.WriteAllText(outputFile.FullName, schemaString);
                                DefaultConsole.Instance.Error.Write($"Schema written to {outputFile.FullName}\n");
                            }
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
