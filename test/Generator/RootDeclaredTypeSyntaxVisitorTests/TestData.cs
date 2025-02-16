using System.Collections.Generic;
using System.Collections.Immutable;
using SharpSchema.Annotations;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace SharpSchema.Generator.TestData;

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
    public string String { get; set; }

    public int Int { get; set; }

    public bool Bool { get; set; }

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

public class Class_WithArrayProperties
{
    public int[] NumbersArray { get; set; }

    public string?[] Names { get; set; }

    public Address[]? Addresses { get; set; }

    public List<int> NumbersList { get; set; }

    public IEnumerable<string>? StringsEnumerable { get; set; }

    public ImmutableArray<string> StringImmutableArray { get; set; }
}

public class Class_WithInvalidProperties
{
    public string Name { set { } }

    public static string Static { get; set; }

    public virtual string Virtual { get; set; }
}

public class Class_WithDictionaryProperties
{
    public Dictionary<string, int> ValueTypes { get; set; }

    public Dictionary<string, Address> ReferenceTypes { get; set; }

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

public record Record_WithReferenceTypeParameters(
    [SchemaMeta(
        Title = "Address Title",
        Description = "Address Description",
        Comment = "This is just a test")]
    Address Address,
    [SchemaMeta(Deprecated = true)]
    Address? NullableAddress,
    [SchemaMeta(
        Title = "Addresses Title")]
    List<Address>? Addresses);

public record Record_WithParametersAndProperties(string Name, int Age)
{
    public required Address Address { get; set; }
}

public struct Struct_WithNullableValueTypes
{
    public string? String { get; set; }

    public int? Int { get; set; }

    public byte? Byte { get; set; }

    public sbyte? SByte { get; set; }

    public short? Short { get; set; }

    public ushort? UShort { get; set; }

    public uint? UInt { get; set; }

    public long? Long { get; set; }

    public ulong? ULong { get; set; }

    public float? Float { get; set; }

    public double? Double { get; set; }

    public decimal? Decimal { get; set; }

    public char? Char { get; set; }

}

public record Record_WithReferenceTypeProperties
{
    public string Name { get; set; }

    public int Age { get; set; }

    public Person Person { get; set; }
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

public class Class_WithTypeSchemaOverride
{
    public BadOverride BadOverride { get; set; }

    public GoodOverride GoodOverride { get; set; }

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

public class Class_ExtendsAbstractClass : AbstractClass
{
    public override string Name { get; set; }
}

public struct Struct_WithAbstractProperties
{
    public AbstractClass Abstract { get; set; }

    public Class_ExtendsAbstractClass Concrete { get; set; }
}

public record Record_WithAbstractParameters(Card Card, ImmutableArray<Card> Deck);

public record Record_WithGenericAbstractProperty
{
    public MagicStack Stack { get; set; }

    public CardStack<Card.AceOfSpades> CardStack { get; set; }
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

public class Person(string name, int age)
{
    public string Name { get; set; } = name;

    public int Age { get; set; } = age;

    public Office Office { get; set; }
}

public struct Office
{
    public string Name { get; set; }

    public Address Address { get; set; }
}

public abstract class AbstractClass
{
    public virtual string Name { get; set; }

    public int Age { get; set; }
}

public class MagicStack : CardStack<Card.AceOfSpades>
{
    public List<Card.AceOfSpades> Stack = [];

    public override Card.AceOfSpades Peek() => this.Stack[^1];

    public override void Push(Card.AceOfSpades card) => this.Stack.Add(card);
}

public abstract class CardStack<TCard>
    where TCard : Card
{
    public abstract TCard Peek();

    public abstract void Push(TCard card);
}

public abstract record Card(Card.SuitKind Suit, int Value)
{
    public enum SuitKind : byte
    {
        Spades,
        Hearts,
        Clubs,
        Diamonds
    }

    public enum FaceKind
    {
        [SchemaEnumValue("NotFaceCard")]
        None,
        Jack,
        Queen,
        King,
        Ace
    }

    public abstract FaceKind Face { get; }

    public record AceOfSpades() : Card(SuitKind.Spades, 1)
    {
        public override FaceKind Face => FaceKind.Ace;
    }

    public record KingOfSpades() : Card(SuitKind.Spades, 13)
    {
        public override FaceKind Face => FaceKind.King;
    }

    public record QueenOfSpades() : Card(SuitKind.Spades, 12)
    {
        public override FaceKind Face => FaceKind.Queen;
    }

    public record JackOfSpades() : Card(SuitKind.Spades, 11)
    {
        public override FaceKind Face => FaceKind.Jack;
    }

    public record TenOfSpades() : Card(SuitKind.Spades, 10)
    {
        public override FaceKind Face => FaceKind.None;
    }
}
