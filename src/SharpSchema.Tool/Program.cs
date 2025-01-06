// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.CommandLine.IO;

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
    /// <summary>
    /// The entry point of the application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The exit code of the application.</returns>
    public static async Task<int> Main(string[] args)
    {
        // Loader Options
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

        Option<int> directoryRecursionDepthOption = new(
            ["--directory-recursion-depth", "-t"],
            description: "The maximum depth to recurse when searching for assemblies in a reference directory.",
            getDefaultValue: () => 1);

        // Converter Options
        Option<bool> includeInterfacesOption = new(
            "--include-interfaces",
            description: "Include interfaces in the schema.",
            getDefaultValue: () => false);

        Option<bool> enumAsUnderlyingTypeOption = new(
            "--enum-as-underlying-type",
            description: "Use the underlying type of an enum in the schema instead of strings.",
            getDefaultValue: () => false);

        Option<bool> parseDocCommentsOptions = new(
            "--parse-doc-comments",
            description: "Parse XML documentation comments.",
            getDefaultValue: () => false);

        Option<int> maxDepthOption = new(
            "--max-depth",
            description: "The maximum depth to traverse when converting types.",
            getDefaultValue: () => 50);

        // Writer Options
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
            });

        Option<bool> prettyPrintedOption = new(
            "--pretty-printed",
            description: "Pretty print the output JSON.",
            getDefaultValue: () => true);

        Option<bool> strictJsonEscapingOption = new(
            "--strict-json-escaping",
            description: "Use strict JSON escaping.",
            getDefaultValue: () => false);

        Option<bool> overwriteOption = new(
            "--overwrite",
            description: "Overwrite the output file if it exists.",
            getDefaultValue: () => false);

        RootCommand rootCommand = new("Generates a JSON schema from a .NET class.")
        {
            assemblyOption,
            classNameOption,
            referenceOption,
            referenceDirectoryOption,
            directoryRecursionDepthOption,
            includeInterfacesOption,
            enumAsUnderlyingTypeOption,
            parseDocCommentsOptions,
            maxDepthOption,
            outputOption,
            prettyPrintedOption,
            strictJsonEscapingOption,
            overwriteOption,
        };

        rootCommand.SetHandler(ic =>
        {
            FileInfo assemblyFile = ic.ParseResult.GetValueForOption(assemblyOption) ?? throw new InvalidOperationException("Assembly file not provided.");
            string[] classNames = ic.ParseResult.GetValueForOption(classNameOption) ?? throw new InvalidOperationException("Class name not provided.");

            GenerateCommandHandler.LoaderOptions loaderOptions = new(
                ic.ParseResult.GetValueForOption(directoryRecursionDepthOption),
                [.. ic.ParseResult.GetValueForOption(referenceOption) ?? [], assemblyFile],
                [.. ic.ParseResult.GetValueForOption(referenceDirectoryOption) ?? []]);

            GenerateCommandHandler.ConverterOptions converterOptions = new(
                ic.ParseResult.GetValueForOption(includeInterfacesOption),
                ic.ParseResult.GetValueForOption(enumAsUnderlyingTypeOption),
                ic.ParseResult.GetValueForOption(parseDocCommentsOptions),
                ic.ParseResult.GetValueForOption(maxDepthOption));

            GenerateCommandHandler.WriterOptions writerOptions = new(
                ic.ParseResult.GetValueForOption(outputOption),
                ic.ParseResult.GetValueForOption(prettyPrintedOption),
                ic.ParseResult.GetValueForOption(strictJsonEscapingOption),
                ic.ParseResult.GetValueForOption(overwriteOption));

            GenerateCommandHandler handler = new(DefaultConsole.Instance, loaderOptions, converterOptions, writerOptions);

            ic.ExitCode = (int)handler.Invoke(classNames, assemblyFile);
        });

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