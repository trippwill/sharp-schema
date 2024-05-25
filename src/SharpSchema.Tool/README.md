# SharpSchema.Tool

SharpSchema.Tool is a .NET command-line tool for generating JSON schemas from .NET types.

## Installation

You can install SharpSchema.Tool globally using the following command:

dotnet tool install -g SharpSchema.Tool

This will make the `sharpschema` command available globally in your command line.

## Usage

You can use the `sharpschema` command to generate a JSON schema from a .NET type. Here's the basic usage:

`sharpschema generate -a <assembly-path> -c <type-name>`

- `<assembly-path>`: The path to the .NET assembly (.dll file) that contains the type.
- `<type-name>`: The full name of the type (including the namespace).

For example, if you have a type `MyNamespace.MyType` in an assembly at `./bin/Debug/net8.0/MyAssembly.dll`, you would use the following command:

`sharpschema generate -a ./bin/Debug/net8.0/MyAssembly.dll -c MyNamespace.MyType`

This will output the JSON schema to the console. If you want to save the schema to a file, you can redirect the output like this:

`sharpschema generate -a ./bin/Debug/net8.0/MyAssembly.dll -c MyNamespace.MyType  -o .`

## Integration into a Build

You can integrate SharpSchema.Tool into your build process by adding a post-build event that runs the `sharpschema` command. In your `.csproj` file, you can add the following:

```
<Target Name="PostBuild" AfterTargets="PostBuildEvent">
  <Exec Command="dotnet tool run sharpschema generate -a $(TargetPath) -c MyNamespace.MyType -o ." />
</Target>
```

This will run the `sharpschema` command after every build, generating a JSON schema for `MyNamespace.MyType` and saving it to `schema.json`.

Please replace `MyNamespace.MyType` with the actual type you want to generate a schema for.