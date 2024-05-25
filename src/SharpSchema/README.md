# SharpSchema (Core Library)

## Conversion Rules

### General

1. Object graph depth is limited to 50 levels by default. This can be overridden using the `MaxDepth` property of `ConveterContext`.
1. Preference is for tight schemas with no extra properties or items.

### Names

1. Property names are camelCased.
1. Enum values are kebab-cased.
1. '+' and '`' are replaced with '_'.
1. Overrides can be provided using the `System.Text.Json.JsonPropertyNameAttribute`.

### Schema Root

A schema root class or struct must be annotated with `SharpSchema.Annotations.SchemaRootAttribute`.

### Objects

1. Whether a property is required is determined by the nullability of the property.
    1. It is assumed that *nullable reference types* are enabled for all input assemblies.
    1. A `Nullable<T>` will be optional.
    1. A `T?` will be optional.
    1. Use `SharpSchema.Annotations.SchemaRequiredAttribute` to make a property with a nullable type required.
        1. This will generate a `OneOf` schema, allowing either the schema of the type, or `null`. The property will also be listed as `required`. 
    1. Use `SharpSchema.Annotations.SchemaRequiredAttribute(false)` to make a property with a non-nullable type optional.

1. All public properties with a public getter will be part of the generated schema.
    1. Private properties can be included using `System.Text.Json.JsonIncludeAttribute`.
    1. Public properties can be excluded using `SharpSchema.Annotations.SchemaIgnoreAttribute`.

#### Example

```csharp

using System.Text.Json;
using SharpSchema.Annotations;

public class Example
{
    public bool? Optional { get; set; }
    public bool Required { get; set; }

    [SchemaRequired(false)]
    public bool OptionalWithRequiredAttribute { get; set; }

    [JsonInclude]
    [SchemaRequired]
    private bool? RequiredWithRequiredAttribute { get; set; }

    [SchemaIgnore]
    public bool Ignored { get; set; }
}
```

```json
{
  "type": "object",
  "properties": {
    "optional": {
      "oneOf": [
        {
          "type": "boolean"
        },
        {
          "type": "null"
        }
      ]"
    },
    "required": {
      "type": "boolean"
    },
    "optionalWithRequiredAttribute": {
      "type": "boolean"
    },
    "requiredWithRequiredAttribute": {
      "oneOf": [
        {
          "type": "boolean"
        },
        {
          "type": "null"
        }
      ]
    }"
  },
  "required": [
    "required",
    "requiredWithRequiredAttribute"
  ]
}
```


### Arrays and Enumerable

### Dictionaries

1. Only dictionaries with string keys are supported.

### Numbers

1. All integral types are emitted as `integers`.
1. Floating point types and `decimal` are emitted as `number`.
1. All number schema also have min/max values clamped to the valid .NET range for the type.
  1. The range can be overridden using the `SharpSchema.Annotations.ValueRangeAttribute`.

### Abstract Types & Interfaces

1. Abstract types and interfaces are emitted without properties, as OneOf schemas with all discovered implementing types.
1. Interfaces are not emitted by default.
1. Only types in the same assembly as the abstract type are added to the schema for the abstract type.
1. Only types discovered while enumerating the root objects are added to the schema for the interface type.

### Title and Description

1. The default title of a property is derived from the property name.
1. Title, Description and Comment can be overridden using the `SharpSchema.Annotations.SchemaMetaAttribute`.
