## Conversion Rules

### General

1. Object graph depth is limited to 50 levels.
1. Preference is for tight schemas with no extra properties or items.

### Names

1. Property names are camelCased.
1. Enum values are kebab-cased.
1. '+' and '`' are replaced with '_'.

### Objects

1. Whether a property is required is determined by the nullability of the property.
  1. It is assumed that *nullable reference types* are enabled for all input assemblies.
  1. A `Nullable<T>` will be optional.
  1. A `T?` will be optional.
  1. Use `System.ComponenetModel.DataAnnotations.RequiredAttribute` to make a property with a nullable type required.
    1. This will generate a `OneOf` schema, allowing either the schema of the type, or `null`. The property will also be listed as `required`.

1. All public properties with a public getter will be part of the generated schema.
  1. Private properties can be included using `System.Text.Json.JsonIncludeAttribute`.

### Arrays and Enumerable

### Dictionaries

1. Only dictionaries with string keys are supported.

### Numbers

1. All integral types are emitted as `integers`.
1. Floating point types and `decimal` are emitted as `number`.
1. All number schema also have min/max values clamped to the valid .NET range for the type.
  1. The range can be overridden using the `System.ComponentModel.DataAnnotations.RangeAttribute`.

### Abstract Types & Interfaces

1. Abstract types and interfaces are emitted without properties, as OneOf schemas with all discovered implementing types.
1. Only types in the same assembly as the abstract type are added to the schema for the abstract type.
1. Only types discovered while enumerating the root objects are added to the schema for the interface type.

### Title and Description

1. The `System.ComponentModel.DataAnnotations.DisplayAttribute` is used to set the Title and Description of a property.
  1. The `Name` named argument maps to the Title
  1. The `Description` named argument maps to the Description
1. When there is no Display attribute, the Title is set to the humanized name of the property type.