# SharpSchema.Annotations

SharpSchema.Annotations is a .NET Standard 2.0 library that provides a set of attributes for controlling JSON Schema generation. It's part of the larger SharpSchema project, an opiniated solution for JSON Schema generation in .NET.

## Features

- **Schema Annotations**: Define how your types are translated into JSON Schema with a rich set of attributes.
- **Extensible Type Handling**: Customize how different types are handled during schema generation.
- **Source Package**: To avoid referencing and shipping `SharpSchema.Annotations` in your project, include the source code directly in your project with the `SharpSchema.Annotations.Source` package.

## Getting Started

To install the package, you can use the following command in the Package Manager Console:

```powershell
Install-Package SharpSchema.Annotations -Version 1.0.0
```

Or you can add the following `PackageReference` to your `.csproj` file:

```
<ItemGroup>
    <PackageReference Include="SharpSchema.Annotations" Version="<package_version>" />
</ItemGroup>
```

## Usage

After installing the package, you can use the provided attributes to control how your types are translated into JSON Schema. Here's an example:

```csharp
using SharpSchema.Annotations;

[SchemaRoot(Id = "http://example.com/schemas/Person")]
public class Person
{
    [SchemaMeta(Title = "First Name")]
    public string FirstName { get; set; }

    [SchemaMeta(Title = "Last Name")]
    public string LastName { get; set; }

    [SchemaIgnore]
    public string InternalId { get; set; }
}
```

In this example, the `Person` class will be translated into a JSON Schema with an ID of "http://example.com/schemas/Person". The `FirstName` and `LastName` properties will have titles in the schema, and the `InternalId` property will be ignored.

## Contributing

If you have any issues or want to contribute, please open an issue or a pull request on the [GitHub repository](https://github.com/yourusername/SharpSchema.Annotations).

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
