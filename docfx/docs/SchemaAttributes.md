# SharpSchema Attributes

## SharpSchema Annotations

This document describes various attributes in the SharpSchema.Annotations namespace and provides examples of how they can be used to change the default output.

### SchemaConstAttribute

Specifies a constant value for a schema property.

**Example:**

```csharp
[SchemaConst(42)]
public int ConstantValue { get; set; }
```

### SchemaEnumValueAttribute

Specifies a value for an enum member in a schema.

**Example:**

```csharp
public enum Status
{
    [SchemaEnumValue("active")]
    Active,
    [SchemaEnumValue("inactive")]
    Inactive
}
```

### SchemaFormatAttribute

Specifies a format for a schema property.

**Example:**

```csharp
[SchemaFormat("date-time")]
public string Timestamp { get; set; }
```

### SchemaIgnoreAttribute

Indicates that a property should be ignored in the schema.

**Example:**

```csharp
[SchemaIgnore]
public string IgnoredProperty { get; set; }
```

### SchemaLengthRangeAttribute

Specifies the minimum and maximum length for a schema property.

**Example:**

```csharp
[SchemaLengthRange(Min = 5, Max = 10)]
public string LengthRestrictedProperty { get; set; }
```

### SchemaMetaAttribute

Provides metadata for a schema.

**Example:**

```csharp
[SchemaMeta(Title = "Person", Description = "Represents a person.")]
public class Person
{
    public string Name { get; set; }
}
```

### SchemaOverrideAttribute

Overrides the schema with a custom value.

**Example:**

```csharp
[SchemaOverride("{ 'type': 'string', 'maxLength': 50 }")]
public string CustomSchemaProperty { get; set; }
```

### SchemaPropertiesRangeAttribute

Specifies the minimum and maximum number of properties allowed in a schema.

**Example:**

```csharp
[SchemaPropertiesRange(Min = 1, Max = 5)]
public class PropertyRangeClass
{
    public string Property1 { get; set; }
    public string Property2 { get; set; }
}
```

### SchemaRegexAttribute

Specifies a regular expression pattern that a property value must match.

**Example:**

```csharp
[SchemaRegex("^[a-zA-Z0-9]*$")]
public string AlphanumericProperty { get; set; }
```

### SchemaRequiredAttribute

Indicates whether a property is required in a schema.

**Example:**

```csharp
[SchemaRequired]
public string RequiredProperty { get; set; }
```

### SchemaRootAttribute

Marks a class or struct as a schema root.

**Example:**

```csharp
[SchemaRoot(Filename = "person.schema.json", Id = "http://example.com/person")]
public class Person
{
    public string Name { get; set; }
}
```

### SchemaValueRangeAttribute

Specifies a value range for a schema property.

**Example:**

```csharp
[SchemaValueRange(Min = 1.0, Max = 100.0)]
public double ValueRangeProperty { get; set; }
```
