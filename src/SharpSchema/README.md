# SharpSchema

SharpSchema is a very opiniated tool for transforming C# class hierarchies into JSON schema.

It is designed to work with your `System.Text.Json` deserialization library. `SchemaSharp.Annotations` provides
attributes you can apply to your DTOs to express JSON-Schema validation constraints that aren't possible
to express with pure C#.

## Examples

### SimplePerson

```csharp
namespace SharpSchema.Tests;

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

* `$schema`

  Currently, only draft-07 is supported.

* `properties`

  Each property name is camel-cased by default.

    * `surname`
        * `$comment`

          By default, the comment is the .NET type name.

        * `title`

          By default, the title is the property name converted to title case.