# SharpSchema

![NuGet Version](https://img.shields.io/nuget/v/SharpSchema)
[![codecov](https://codecov.io/gh/trippwill/sharp-schema/graph/badge.svg?token=GGB46BYHZ8)](https://codecov.io/gh/trippwill/sharp-schema)

SharpSchema is a .NET library for generating JSON schemas from .NET types. It provides a flexible and extensible API for customizing the generated schemas, and includes a command-line tool for generating schemas from compiled .NET assemblies.

## Features

- Generate JSON schemas from .NET types.
- Customize the generated schemas with attributes.
- Generate schemas from compiled .NET assemblies with the command-line tool.
- Handle complex types, dictionary types, enum types, and more.
- Support for nullable types and required properties.
- Support for property name customization.
- Support for property constraints.

## Installation

You can install the SharpSchema library via NuGet:

```
dotnet add package SharpSchema
```

You can also install the SharpSchema command-line tool globally:

```
dotnet tool install -g SharpSchema.Tool
```

## Usage

### Library

Here's a basic example of how to use the SharpSchema library to generate a JSON schema from a .NET type:

```csharp
using SharpSchema;

var rootTypeContext = RootTypeContext.FromType(typeof(MyRoot));

TypeConverter converter = new(new TypeConverter.Options
{
    EnumAsUnderlyingType = true,
    MaxDepth = 10,
});

JsonSchema schema = converter
    .Convert(rootTypeContext)
    .Build();
```

You can customize the generated schema with attributes from the `SharpSchema.Annotations` namespace:

```
using SharpSchema.Annotations;

[SchemaRequired]
public int MyProperty { get; set; }
```

### Command-Line Tool

You can use the `sharpschema` command to generate a JSON schema from a .NET type. Here's the basic usage:

```
sharpschema generate -a <assembly-path> -c <type-name>
```

- `<assembly-path>`: The path to the .NET assembly (.dll file) that contains the type.
- `<type-name>`: The full name of the type (including the namespace).

For example:

```
sharpschema generate -a ./bin/Debug/net8.0/MyAssembly.dll -c MyNamespace.MyType
```

## Contributing

Contributions are welcome! Please read our [contributing guidelines](CONTRIBUTING.md) for details on how to contribute to the project.

## License

SharpSchema is licensed under the MIT license. See the [LICENSE](LICENSE) file for details.