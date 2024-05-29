# SharpSchema.Tool

## Installation

You can install SharpSchema.Tool globally using the following command:

dotnet tool install -g SharpSchema.Tool

This will make the `sharpschema` command available globally in your command line.

[More about .NET tools packages](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools)

## Usage

You can use the `sharpschema` command to generate a JSON schema from a .NET type. Here's the basic usage:

`sharpschema generate -a <assembly-path> -c <type-name>`

- `<assembly-path>`: The path to the .NET assembly (.dll file) that contains the type.
- `<type-name>`: The full name of the type (including the namespace).

For example, if you have a type `MyNamespace.MyType` in an assembly at `./bin/Debug/net8.0/MyAssembly.dll`, you would use the following command:

`sharpschema generate -a ./bin/Debug/net8.0/MyAssembly.dll -c MyNamespace.MyType`

This will output the JSON schema to the console. If you want to save the schema to a file, you can redirect the output like this:

`sharpschema generate -a ./bin/Debug/net8.0/MyAssembly.dll -c MyNamespace.MyType  -o .`

### Adding reference assemblies

Use `-r <assembly-path>` to add a reference assembly required to load the primary assembly. Use one `-r` for each reference assembly.

If many reference assemblies are in a single directory, use `-d <path-to-directory>` to add all the assemblies in that directory as reference assemblies. The `-t` option can be used to set the directory recursion depth.

For example, when the primary assembly is a .NET Framework assemply, you may want to reference all the assemblies in the BCL:

`sharpschema generate -a ./bin/Debug/net8.0/MyAssembly.dll -c MyNamespace.MyType  -o . -d "C:\Windows\Microsoft.NET\Framework\v4.0.30319" -t 2`

## Other options

Run `sharpschema --help` for the full list of available options.
