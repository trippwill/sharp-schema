// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Humanizer;
using Json.More;
using Json.Schema;
using Xunit;
using Xunit.Abstractions;

namespace SharpSchema.Tests;

public class TypeExtensionsTests(ITestOutputHelper output)
{
    private readonly JsonSerializerOptions writeOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public ITestOutputHelper Output { get; } = output;

    [Fact]
    public void ToJsonSchema_WithBooleanType_ReturnsBooleanSchema()
    {
        // Arrange
        Type type = typeof(bool);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Boolean, schema.GetJsonType());
    }

    [Fact]
    public void ToJsonSchema_WithByteType_ReturnsIntegerSchemaWithMinMaxValues()
    {
        // Arrange
        Type type = typeof(byte);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Integer, schema.GetJsonType());
        Assert.Equal(byte.MinValue, schema.GetMinimum());
        Assert.Equal(byte.MaxValue, schema.GetMaximum());
    }

    [Fact]
    public void ToJsonSchema_WithCharType_ReturnsIntegerSchemaWithMinMaxValues()
    {
        // Arrange
        Type type = typeof(char);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Integer, schema.GetJsonType());
        Assert.Equal(char.MinValue, schema.GetMinimum());
        Assert.Equal(char.MaxValue, schema.GetMaximum());
    }

    [Fact]
    public void ToJsonSchema_WithDateTimeType_ReturnsStringSchemaWithDateTimeFormat()
    {
        // Arrange
        Type type = typeof(System.DateTime);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.String, schema.GetJsonType());
        Assert.Equal(Formats.DateTime, schema.GetFormat());
    }

    [Fact]
    public void ToJsonSchema_WithDoubleType_ReturnsNumberSchemaWithMinMaxValues()
    {
        // Arrange
        Type type = typeof(double);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Number, schema.GetJsonType());
        Assert.Equal(decimal.CreateSaturating(double.MinValue), schema.GetMinimum());
        Assert.Equal(decimal.CreateSaturating(double.MaxValue), schema.GetMaximum());
    }

    [Fact]
    public void ToJsonSchema_WithFloatType_ReturnsNumberSchemaWithMinMaxValues()
    {
        // Arrange
        Type type = typeof(float);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Number, schema.GetJsonType());
        Assert.Equal(decimal.CreateSaturating(float.MinValue), schema.GetMinimum());
        Assert.Equal(decimal.CreateSaturating(float.MaxValue), schema.GetMaximum());
    }

    [Fact]
    public void ToJsonSchema_WithIntType_ReturnsIntegerSchemaWithMinMaxValues()
    {
        // Arrange
        Type type = typeof(int);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Integer, schema.GetJsonType());
        Assert.Equal(int.MinValue, schema.GetMinimum());
        Assert.Equal(int.MaxValue, schema.GetMaximum());
    }

    [Fact]
    public void ToJsonSchema_WithLongType_ReturnsIntegerSchemaWithMinMaxValues()
    {
        // Arrange
        Type type = typeof(long);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Integer, schema.GetJsonType());
        Assert.Equal(long.MinValue, schema.GetMinimum());
        Assert.Equal(long.MaxValue, schema.GetMaximum());
    }

    [Fact]
    public void ToJsonSchema_WithSByteType_ReturnsIntegerSchemaWithMinMaxValues()
    {
        // Arrange
        Type type = typeof(sbyte);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Integer, schema.GetJsonType());
        Assert.Equal(sbyte.MinValue, schema.GetMinimum());
        Assert.Equal(sbyte.MaxValue, schema.GetMaximum());
    }

    [Fact]
    public void ToJsonSchema_WithShortType_ReturnsIntegerSchemaWithMinMaxValues()
    {
        // Arrange
        Type type = typeof(short);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Integer, schema.GetJsonType());
        Assert.Equal(short.MinValue, schema.GetMinimum());
        Assert.Equal(short.MaxValue, schema.GetMaximum());
    }

    [Fact]
    public void ToJsonSchema_WithStringType_ReturnsStringSchema()
    {
        // Arrange
        Type type = typeof(string);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.String, schema.GetJsonType());
    }

    [Fact]
    public void ToJsonSchema_WithUShortType_ReturnsIntegerSchemaWithMinMaxValues()
    {
        // Arrange
        Type type = typeof(ushort);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Integer, schema.GetJsonType());
        Assert.Equal(ushort.MinValue, schema.GetMinimum());
        Assert.Equal(ushort.MaxValue, schema.GetMaximum());
    }

    [Fact]
    public void ToJsonSchema_WithUIntType_ReturnsIntegerSchemaWithMinMaxValues()
    {
        // Arrange
        Type type = typeof(uint);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Integer, schema.GetJsonType());
        Assert.Equal(uint.MinValue, schema.GetMinimum());
        Assert.Equal(uint.MaxValue, schema.GetMaximum());
    }

    [Fact]
    public void ToJsonSchema_WithULongType_ReturnsIntegerSchemaWithMinMaxValues()
    {
        // Arrange
        Type type = typeof(ulong);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Integer, schema.GetJsonType());
        Assert.Equal(ulong.MinValue, schema.GetMinimum());
        Assert.Equal(ulong.MaxValue, schema.GetMaximum());
    }

    [Fact]
    public void ToJsonSchema_WithGuidType_ReturnsStringSchemaWithUuidFormat()
    {
        // Arrange
        Type type = typeof(Guid);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.String, schema.GetJsonType());
        Assert.Equal(Formats.Uuid, schema.GetFormat());
    }

    [Fact]
    public void ToJsonSchema_WithUriType_ReturnsStringSchemaWithUriFormat()
    {
        // Arrange
        Type type = typeof(Uri);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.String, schema.GetJsonType());
        Assert.Equal(Formats.Uri, schema.GetFormat());
    }

    [Fact]
    public void ToJsonSchema_WithEnumType_ReturnsStringSchemaWithEnumValues()
    {
        // Arrange
        Type type = typeof(DayOfWeek);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.String, schema.GetJsonType());
        Assert.Equal(Enum.GetNames(type).Select(name => name.Kebaberize()), schema.GetEnum()!.Select(n => n!.ToString()));
    }

    [Fact]
    public void ToJsonSchema_WithDateTimeOffsetType_ReturnsStringSchemaWithDateTimeFormat()
    {
        // Arrange
        Type type = typeof(DateTimeOffset);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.String, schema.GetJsonType());
        Assert.Equal(Formats.DateTime, schema.GetFormat());
    }

    [Fact]
    public void ToJsonSchema_WithIntArrayType_ReturnsArraySchemaWithIntegerItems()
    {
        // Arrange
        Type type = typeof(int[]);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Array, schema.GetJsonType());
        Assert.Equal(SchemaValueType.Integer, schema.GetItems()!.GetJsonType());
    }

    [Fact]
    public void ToJsonSchema_WithStringArrayType_ReturnsArraySchemaWithStringItems()
    {
        // Arrange
        Type type = typeof(string[]);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Array, schema.GetJsonType());
        Assert.Equal(SchemaValueType.String, schema.GetItems()!.GetJsonType());
    }

    [Theory]
    [InlineData(typeof(Dictionary<string, int>))]
    [InlineData(typeof(ImmutableDictionary<string, int>))]
    [InlineData(typeof(IDictionary<string, int>))]
    [InlineData(typeof(IReadOnlyDictionary<string, int>))]
    [InlineData(typeof(IImmutableDictionary<string, int>))]
    public void ToJsonSchema_WithDictionaryType_ReturnsObjectSchemaWithAdditionalProperties(Type type)
    {
        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Object, schema.GetJsonType());
        JsonSchema? additionalProperties = schema.GetAdditionalProperties();
        Assert.NotNull(additionalProperties);
        Assert.Equal(SchemaValueType.Integer, additionalProperties.GetJsonType());
    }

    [Fact]
    public void ToJsonSchema_WithObjectWithProperties_ReturnsObjectSchemaWithProperties()
    {
        // Arrange
        Type type = typeof(ObjectWithProperties);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Object, schema.GetJsonType());
        IReadOnlyDictionary<string, JsonSchema>? properties = schema.GetProperties();
        Assert.NotNull(properties);
        Assert.Equal(3, properties.Count);
        Assert.True(properties.ContainsKey("Name".Camelize()));
        Assert.True(properties.ContainsKey("Age".Camelize()));
        Assert.True(properties.ContainsKey("IncludedPrivateProperty".Camelize()));
        Assert.False(properties.ContainsKey("Height".Camelize()));
        Assert.False(properties.ContainsKey("PrivateProperty".Camelize()));
        Assert.False(properties.ContainsKey("IgnoredProperty".Camelize()));
        Assert.Equal(SchemaValueType.String, properties["Name".Camelize()].GetJsonType());
        Assert.Equal(SchemaValueType.Integer, properties["Age".Camelize()].GetJsonType());
        Assert.Equal(new Uri("https://schema.org/object-with-properties"), schema.GetId());
    }

    [Theory]
    [InlineData(typeof(IEnumerable<int>))]
    [InlineData(typeof(ICollection<int>))]
    [InlineData(typeof(IList<int>))]
    [InlineData(typeof(List<int>))]
    [InlineData(typeof(ImmutableList<int>))]
    [InlineData(typeof(IImmutableList<int>))]
    public void ToJsonSchema_WithEnumerableType_ReturnsArraySchemaWithItemType(Type type)
    {
        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Array, schema.GetJsonType());
        Assert.Equal(SchemaValueType.Integer, schema.GetItems()!.GetJsonType());
    }

    [Fact]
    public void ToJsonSchema_WithComplexObjectType_ReturnsObjectSchemaWithRequiredProperties()
    {
        // Arrange
        Type type = typeof(ComplexObject);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Object, schema.GetJsonType());
        IReadOnlyList<string>? required = schema.GetRequired();
        Assert.NotNull(required);
        Assert.Equal(4, required.Count);
        Assert.Contains("name", required);
        Assert.Contains("age", required);
        Assert.Contains("forceRequiredValue", required);
        Assert.Contains("forceRequiredReference", required);
        Assert.DoesNotContain("optionalProperty", required);
        Assert.DoesNotContain("height", required);
    }

    [Fact]
    public void ToJsonSchema_WithNonStringKey_ThrowsArgumentException()
    {
        Type type = typeof(Dictionary<int, int>);
        Assert.Throws<ArgumentException>(() => type.ToJsonSchema());
    }

    [Fact]
    public void ToJsonSchema_WithExceedingMaxDepth_ThrowsArgumentException()
    {
        // Arrange
        Type type = typeof(ExceedingMaxDepthClass);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => type.ToJsonSchema());
    }

    [Fact]
    public void ToJsonSchema_WithRecordType_ReturnsObjectSchemaWithProperties()
    {
        // Arrange
        Type type = typeof(SampleRecord);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Object, schema.GetJsonType());
        Assert.Contains("id", schema.GetRequired()!);
        Assert.Contains("name", schema.GetRequired()!);
    }

    [Fact]
    public void ToJsonSchema_WithAbstractType_ReturnsObjectSchemaWithOneOfConcreteTypes()
    {
        // Arrange
        Type type = typeof(AbstractClass);

        // Act
        JsonSchema actualSchema = type.ToJsonSchema();
        this.OutputSchema(actualSchema);

        // Assert
        Assert.Equal(2, actualSchema.GetOneOf()!.Count);
    }

    [Fact]
    public void ToJsonSchema_WithRootClass_ReturnsObjectSchemaWithProperties()
    {
        // Arrange
        Type type = typeof(RootClass);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Object, schema.GetJsonType());
        Assert.Contains("objectMap", schema.GetRequired()!);
        Assert.Contains("complexObjectMap", schema.GetRequired()!);
    }

    [Fact]
    public void ToJsonSchema_WithAmbientSchema_ReturnsObjectSchemaWithProperties()
    {
        // Arrange
        Type type = typeof(AmbientClass);

        // Act
        JsonSchema schema = type.ToJsonSchema();
        this.OutputSchema(schema);

        // Assert
        Assert.Equal(SchemaValueType.Object, schema.GetJsonType());
        Assert.Contains("name", schema.GetProperties()!);
    }

    [Theory]
    [MemberData(nameof(SchemaEvaluate_WithInput_ReturnsExpectedResult_Data))]
    public void SchemaEvaluate_WithInput_ReturnsExpectedResult(string caseName, string input, bool expectedIsValid)
    {
        _ = caseName;

        // Arrange
        JsonSchema schema = typeof(ObjectWithProperties).ToJsonSchema();

        // Act
        EvaluationResults results = schema.Evaluate(JsonDocument.Parse(input).RootElement, new EvaluationOptions()
        {
            OutputFormat = OutputFormat.List,
        });

        if (!results.IsValid)
        {
            this.Output.WriteLine(JsonSerializer.Serialize(results, this.writeOptions));
        }

        // Assert
        Assert.Equal(expectedIsValid, results.IsValid);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Test Code")]
    public static TheoryData<string, string, bool> SchemaEvaluate_WithInput_ReturnsExpectedResult_Data() => new()
    {
        {
            "Name Only",
            /* lang=json */"""
            {
                "name": "Test Name"
            }
            """,
            false
        },
        {
            "Age Only",
            /* lang=json */"""
            {
                "age": 42
            }
            """,
            false
        },
        {
            "Name, Age as String",
            /* lang=json */"""
            {
                "name": "Test Name",
                "age": "42"
            }
            """,
            false
        },
        {
            "Name and Age",
            /* lang=json */"""
            {
                "name": "Test Name",
                "age": 42
            }
            """,
            true
        },
        {
            "Name, Age, and Height",
            /* lang=json */"""
            {
                "name": "Test Name",
                "age": 42,
                "height": 5.8
            }
            """,
            false
        },
        {
            "Name, Age, and Ignored Property",
            /* lang=json */"""
            {
                "name": "Test Name",
                "age": 42,
                "ignoredProperty": 3.14
            }
            """,
            false
        },
        {
            "Name, Age, and Private Property",
            /* lang=json */"""
            {
                "name": "Test Name",
                "age": 42,
                "privateProperty": "Private Value"
            }
            """,
            false
        },
        {
            "Name, Age, and Included Private Property",
            /* lang=json */"""
            {
                "name": "Test Name",
                "age": 42,
                "includedPrivateProperty": "Included Private Value"
            }
            """,
            true
        },
        {
            "Name, Age, Included Private Property as null",
            /* lang=json */"""
            {
                "name": "Test Name",
                "age": 42,
                "includedPrivateProperty": null
            }
            """,
            true
        },
    };

    private void OutputSchema(JsonSchema schema)
    {
        this.Output.WriteLine(
            JsonSerializer.Serialize(
                schema.ToJsonDocument().RootElement,
                this.writeOptions));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Test Code")]
    [Description("A class with an age property")]
    internal interface IHasAge
    {
        int Age { get; }
    }

    internal class ExceedingMaxDepthClass
    {
        public ExceedingMaxDepthClass? Nested { get; set; }
    }

    private class RootClass
    {
        [RegularExpression("[a-z]+")]
        public IReadOnlyDictionary<string, ObjectWithProperties> ObjectMap { get; } = new Dictionary<string, ObjectWithProperties>();

        public IReadOnlyDictionary<string, ComplexObject> ComplexObjectMap { get; } = new Dictionary<string, ComplexObject>();

        public Dictionary<string, AbstractClass> AbstractClassMap { get; } = [];

        public IEnumerable<ConcreteClass1> ConcreteClass1List { get; } = [];
    }

    [DisplayName("https://schema.org/object-with-properties")]
    private class ObjectWithProperties : IHasAge
    {
        [Description("This is for a $comment property")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Object Age", Description = "The age of the object")]
        public int Age { get; set; }

        // Properties with private getters are ignored
        public float Height { private get; set; }

        [JsonIgnore]
        public decimal IgnoredProperty { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test Code")]
        private string? PrivateProperty { get; set; }

        [JsonInclude]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test Code")]
        private string? IncludedPrivateProperty { get; set; }

        // Indexers are ignored
        public int this[int i] => 0;
    }

    private class ComplexObject
    {
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }

        public float? Height { get; set; }

        public ObjectWithProperties? OptionalProperty { get; set; }

        public AbstractClass? AbstractProperty { get; set; }

        [JsonRequired]
        [Range(2.6, 300.5)]
        public decimal? ForceRequiredValue { get; set; }

        [JsonRequired]
        [JsonInclude]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test Code")]
        private IHasAge? ForceRequiredReference { get; set; }
    }

    [AmbientValue(Schema)]
    private class AmbientClass
    {
        private const string Schema = /* lang=json */"""
            {
                "type": "object",
                "properties": {
                    "name": {
                        "type": "string"
                    }
                }
            }
            """;
    }

    public record SampleRecord(int Id, string Name);

    public abstract class AbstractClass
    {
        public string Name { get; set; } = string.Empty;
    }

    public class ConcreteClass1 : AbstractClass
    {
        public int Age { get; set; }
    }

    public class ConcreteClass2 : AbstractClass
    {
        public float Height { get; set; }
    }
}
