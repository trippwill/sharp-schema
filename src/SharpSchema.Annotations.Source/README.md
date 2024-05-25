# SharpSchema.Annotations.Source

This is the source-only package for SharpSchema.Annotations. It allows you to include the source code of SharpSchema.Annotations in your project without adding an assembly reference.

## Getting Started

To install the package, you can use the following command in the Package Manager Console:

```powershell
Install-Package SharpSchema.Annotations.Source -Version 1.0.0
```

Or you can add the following `PackageReference` to your `.csproj` file:

```
<ItemGroup>
    <PackageReference Include="SharpSchema.Annotations.Source" Version="<package_version>" PrivateAssets="all" />
</ItemGroup>
```

## Usage

When you add a `PackageReference` to this package, the source code of SharpSchema.Annotations will be included in your project. You can use the types in SharpSchema.Annotations as if they were part of your project.

## Contributing

If you have any issues or want to contribute, please open an issue or a pull request on the [GitHub repository](https://github.com/tripwill/sharp-schema).

## License

This project is licensed under the MIT License. See the [LICENSE](../../LICENSE) file for details.
