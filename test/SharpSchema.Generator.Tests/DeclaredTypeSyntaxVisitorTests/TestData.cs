using System;
using System.Collections.Generic;
using SharpSchema.Annotations;

namespace SharpSchema.Generator.Tests.DeclaredTypeSyntaxVisitorTests;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

public class Class_WithDocComments
{
    /// <jsonschema>
    ///     <title>The name of the person.</title>
    /// </jsonschema>
    public string Name { get; init; }

    /// <jsonschema>
    ///     <description>The age of the person.</description>
    /// </jsonschema>
    public int Age { get; set; }
}

public class Class_WithValueTypes
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class Class_WithIEnumerableProperty
{
    public List<int> Numbers { get; set; }
}

public class Class_WithDictionaryOfValueTypes
{
    public Dictionary<string, int> Data { get; set; }
}

public class Class_WithDictionaryOfReferenceTypes
{
    public Dictionary<string, Address> Data { get; set; }
}

/// <summary>
/// Demonstrates param-based XML metadata.
/// </summary>
/// <param name="Name">
///     <jsonschema>
///         <title>NameOfRecord</title>
///         <description>The record's name.</description>
///         <example>John Doe</example>
///     </jsonschema>
/// </param>
/// <param name="Age">
///     <jsonschema>
///         <title>AgeOfRecord</title>
///         <description>The record's age.</description>
///         <example>42</example>
///     </jsonschema>
/// </param>
public record Record_WithValueTypeParameters(string Name, int Age = 42);

public record Record_WithReferenceTypeParameters(Address Address, Address? NullableAddress, List<Address>? Addresses);

public struct Struct_WithNullableValueTypes
{
    public string? Name { get; set; }
    public int? Age { get; set; }
}

public record Record_WithReferenceTypeProperties
{
    public string Name { get; set; }

    public int Age { get; set; }

    public Address Address { get; set; }
}

public class Class_WithSchemaOverride
{
    [SchemaOverride("{\"type\":\"string\",\"maxLength\":50}")]
    public string Name { get; set; }

    [SchemaOverride("{\"type\":\"integer\",\"minimum\":0}")]
    public int Age { get; set; }
}

public record Record_WithSchemaOverride(
    [property: SchemaOverride("{\"type\":\"string\",\"maxLength\":50}")] string Name,
    [property: SchemaOverride("{\"type\":\"integer\",\"minimum\":0}")] int Age
);

public class Class_WithTypeSchemaOverride
{
    public Custom Custom { get; set; }
}

[SchemaOverride("{\"type\":\"object\",\"properties\":{\"custom\":\"string\"}}")]
public record Custom();

public record Address
{
    public string Street { get; set; }
    public string City { get; set; }
}
