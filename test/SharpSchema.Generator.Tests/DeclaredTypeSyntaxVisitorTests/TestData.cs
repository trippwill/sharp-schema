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

    public bool IsAdult { get; set; }

    public byte Byte { get; set; }

    public sbyte SByte { get; set; }

    public short Short { get; set; }

    public ushort UShort { get; set; }

    public uint UInt { get; set; }

    public long Long { get; set; }

    public ulong ULong { get; set; }

    public float Float { get; set; }

    public double Double { get; set; }

    public decimal Decimal { get; set; }

    public char Char { get; set; }
}

public class Class_WithIEnumerableProperty
{
    public List<int> Numbers { get; set; }
}

public class Class_WithArrayProperty
{
    public int[] Numbers { get; set; }

    public string?[] Names { get; set; }

    public Address[]? Addresses { get; set; }
}

public class Class_WithInvalidProperties
{
    public string Name { set { } }

    public static string Static { get; set; }

    public virtual string Virtual { get; set; }
}

public class Class_WithDictionaryOfValueTypes
{
    public Dictionary<string, int> Data { get; set; }
}

public class Class_WithDictionaryOfReferenceTypes
{
    public Dictionary<string, Address> Data { get; set; }
}

public class Class_WithUnsupportedDictionaryKey
{
    public Dictionary<Address, int> Data { get; set; }
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
    [SchemaOverride("{\"type\":\"string\",\"maxLength\":50}")] string Name,
    [SchemaOverride("{\"type\":\"integer\",\"minimum\":0}")] int Age
);

public class Class_WithBadTypeSchemaOverride
{
    public BadOverride Custom { get; set; }
}

public class Class_WithTypeSchemaOverride
{
    public GoodOverride Custom { get; set; }
}

public class Class_WithIgnoredProperty
{
    [SchemaIgnore]
    public string Ignored { get; set; }

    public string NotIgnored { get; set; }
}

public record Record_WithIgnoredParameter(
    [SchemaIgnore] string Ignored,
    string NotIgnored
);

public record Class_WithInternalProperties
{
    internal string Internal { get; set; }

    protected string Protected { get; set; }

    private string Private { get; set; }
}

public class Class_WithRequiredProperties
{
    public required int? RequiredInt { get; set; }

    public string Required { get; set; }

    public string? Optional { get; set; }

    [SchemaRequired]
    public string? Default { get; set; } = "default";
}

[SchemaOverride("{invalidJson}")]
public record BadOverride();

[SchemaOverride(/* lang=json */ """
    {
      "type": "object",
        "properties": {
            "custom": {
                "type": "string"
            }
        }
    }
    """)]
public record GoodOverride();

public record Address
{
    public string Street { get; set; }
    public string City { get; set; }
}
