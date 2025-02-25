using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;
using SharpSchema.Annotations;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace SharpSchema.Generator.TestData;

using Test.Generator.RootSyntaxVisitorTests;


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

public record Record_WithValueParameters(string Name, int Age);

public record Record_WithDefaultValueParameter(string Name, int Age = 42);

public record Record_WithNullableParameters(string? Name, int? Age);

public record Record_WithValueParametersAndProperty(string Name, int Age)
{
    public string Title { get; set; }
}

public record Record_WithValueParametersAndPropertyInitializer(string Name, int Age)
{
    public string Title { get; set; } = "How to make a record";
}

public record Record_WithDefaultValueParametersAndConstantProperty(string Name, int Age = 42)
{
    public string Title => "How to make a record";
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
public record Record_WithDocComments(string Name, int Age = 42);


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
}

public class Class_WithDictionaryProperties
{
    public Dictionary<string, int> ValueTypes { get; set; }

    public Dictionary<string, Address> ReferenceTypes { get; set; }

}

/// <summary>
/// <see cref="VerifyTests.Verify_DictionaryKeyMode(DictionaryKeyMode)"/>
/// </summary>
public class Class_WithUnsupportedDictionaryKey
{
    public Dictionary<Address, int> Data { get; set; }
}


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

    [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
    public string AlsoIgnored { get; set; }

    public string NotIgnored { get; set; }
}

public record Record_WithIgnoredParameter(
    [SchemaIgnore] string Ignored,
    string NotIgnored
);

/// <summary>
/// <see cref="VerifyTests.Verify_AccessibilityMode(AccessibilityMode)"/>
/// </summary>
public record Class_WithInternalProperties
{
    public string Public { get; set; }

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
}

public record MagicStack(List<Card> Cards);

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

public class GameHall
{
    public string Name { get; set; }

    public Dictionary<string, GameRoom> Rooms { get; set; }
}

public struct GameRoom
{
    public string Name { get; set; }

    public Table<BaseHand> WildCardTable { get; set; }

    public PokerTable PokerTable { get; set; }

    public BlackjackTable BlackjackTable { get; set; }

    public BridgeTable BridgeTable { get; set; }
}

[SchemaTraversalMode(TraversalMode.Bases)]
public abstract record Table<T>(int PlayerCount) where T : BaseHand
{
    public abstract T? DealerHand { get; }

    public IReadOnlyCollection<T> Hands { get; }
}

[SchemaTraversalMode(TraversalMode.Bases)]
public record PokerTable(int PlayerCount) : Table<BaseHand.Poker>(PlayerCount)
{
    public override BaseHand.Poker DealerHand { get; }
}

[SchemaTraversalMode(TraversalMode.Bases)]
public record BlackjackTable(int PlayerCount) : Table<BaseHand.Blackjack>(PlayerCount)
{
    public override BaseHand.Blackjack DealerHand { get; }
}

[SchemaTraversalMode(TraversalMode.Bases)]
public record BridgeTable(int PlayerCount) : Table<BaseHand.Bridge>(PlayerCount)
{
    public override BaseHand.Bridge? DealerHand => null;
}

public record WhistTable(int PlayerCount) : Table<BaseHand.Bridge>(PlayerCount)
{
    public override BaseHand.Bridge? DealerHand => null;
}

public abstract record BaseHand(int Size)
{
    public abstract string Game { get; }

    public List<Card> Cards { get; set; }

    [SchemaTraversalMode(TraversalMode.Bases)]
    public record Poker() : BaseHand(5)
    {
        public override string Game => "Poker";

        public bool IsRoyalFlush => Cards.Count == Size && Cards.All(c => c.IsFaceCard);
    }

    [SchemaTraversalMode(TraversalMode.Bases)]
    public record Blackjack() : BaseHand(2)
    {
        public override string Game => "Blackjack";

        public int Value => Cards.Sum(c => c.Rank switch
        {
            Card.RankKind.Ace => 11,
            Card.RankKind.Jack => 10,
            Card.RankKind.Queen => 10,
            Card.RankKind.King => 10,
            _ => (int)c.Rank
        });
    }

    [SchemaTraversalMode(TraversalMode.Bases)]
    public record Bridge() : BaseHand(13)
    {
        public override string Game => "Bridge";

        public bool IsNoTrump => Cards.All(c => c.Suit == Card.SuitKind.Spades);
    }
}


public record Card(Card.SuitKind Suit, Card.RankKind Rank)
{
    public bool IsFaceCard => Rank >= RankKind.Jack;

    public enum SuitKind
    {
        Spades,
        Hearts,
        Clubs,
        Diamonds
    }

    public enum RankKind
    {
        Ace,
        Two,
        Three,
        Four,
        Five,
        Six,
        Seven,
        Eight,
        Nine,
        Ten,
        Jack,
        Queen,
        King
    }
}
