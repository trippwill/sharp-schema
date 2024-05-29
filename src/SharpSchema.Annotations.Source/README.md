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
