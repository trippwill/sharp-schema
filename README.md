# SharpSchema

SharpSchema is a .NET library and command-line tool for generating JSON schemas from .NET types.

It leverages the excellent [JsonSchema.NET library](https://github.com/gregsdennis/json-everything).

The CLI tool requires .NET SDK 8.

The `SharpSchema.Annotations` library is a `netstandard2.0` class library that provides a set
of attributes for influencing the output of SharpSchema.

It is also available as source-only package, that will compile the attributes into your target assembly.
Using `SharpSchema.Annotations.Source` means you won't have to ship another dependency.

## Examples

### SimplePerson

```csharp
namespace Contoso;

[SchemaRoot(Filename = "person.schema.json")]
public record SimplePerson(
    string Surname,
    string? FamilyName,
    DateTime DateOfBirth,
    SimplePerson.RoleKind Role)
{
    public enum RoleKind
    {
        User,
        SectionAdmin,
        SystemAdmin,
    }

    [SchemaIgnore]
    public int Age => (int)((DateTime.Now - this.DateOfBirth).TotalDays / 365.25);
}

```

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "surname": {
      "$comment": "string",
      "type": "string",
      "title": "Surname"
    },
    "familyName": {
      "oneOf": [
        {
          "$comment": "string",
          "type": "string"
        },
        {
          "type": "null"
        }
      ],
      "title": "Family Name"
    },
    "dateOfBirth": {
      "$comment": "DateTime",
      "type": "string",
      "format": "date-time",
      "title": "Date of Birth"
    },
    "role": {
      "$comment": "RoleKind",
      "type": "string",
      "enum": [
        "user",
        "section-admin",
        "system-admin"
      ],
      "title": "Role"
    }
  },
  "required": [
    "surname",
    "dateOfBirth",
    "role"
  ],
  "additionalProperties": false
}
```

## Installation

You can install the CLI SharpSchema.Tool globally using the following command:

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

## Integration into a Build

You can integrate SharpSchema.Tool into your build process by adding a post-build event that runs the `sharpschema` command.

First, add `SharpSchema.Tool` as a [dotnet local tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools#install-a-local-tool) to your repository.

Then, in your `.csproj` file, you can add a target similar to the following (this is for the example above):

```xml
<Target Name="GenerateSchema" AfterTargets="PostBuildEvent">
  <ItemGroup>
    <_Reference Include="@(ReferencePath)" Condition="'%(ReferencePath.ResolvedFrom)' != 'ImplicitlyExpandDesignTimeFacades'" />
  </ItemGroup>
  <PropertyGroup>
    <_SharpSchemaCommand>dotnet tool run sharpschema -- -a $(TargetPath) -c Contoso.SimplePerson -o . --overwrite</_SharpSchemaCommand>
  </PropertyGroup>
    <Exec Command="$(_SharpSchemaCommand) @(_Reference->' -r &quot;%(FullPath)&quot;', ' ')" />
</Target>
```

This will run the `sharpschema` command after every build, generating a JSON schema for `Contoso.SimplePerson` and saving it to the current directory with the filename `person.schema.json`.

## Library

Here's a basic example of how to use the SharpSchema library to generate a JSON schema from a .NET type at runtime.

```csharp
using Contoso;
using SharpSchema;

var rootTypeContext = RootTypeContext.FromType(typeof(SimplePerson));

TypeConverter converter = new(new TypeConverter.Options
{
    EnumAsUnderlyingType = true,
    MaxDepth = 10,
});

JsonSchema schema = converter
    .Convert(rootTypeContext)
    .Build();
```

## Contributing

If you have any issues or want to contribute, please open an issue or a pull request on the [GitHub repository](https://github.com/tripwill/sharp-schema). See the [CONTRIBUTING](CONTRIBUTING.md) file for more details.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
