// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Humanizer;
using Json.Schema;
using Xunit;
using Xunit.Abstractions;
using SharpSchema.Annotations;

namespace SharpSchema.Tests;

public class JsonSchemaBuilderExtensionsTests(ITestOutputHelper outputHelper) : TestBase(outputHelper)
{
    [Fact]
    public void AddType_Enum_ReturnsStringSchemaWithEnumValues()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        Type type = typeof(TestEnum);
        string[] expectedEnumValues = ["value1", "value2", "value3"];

        // Act
        JsonSchema result = builder.AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(SchemaValueType.String, result.GetJsonType());
        Assert.Equal(expectedEnumValues, result.GetEnum()!.Select(jn => jn!.ToString()));
    }

    [Theory]
    [InlineData(typeof(bool), SchemaValueType.Boolean)]
    [InlineData(typeof(char), SchemaValueType.String, Skip = "Not currently supported")]
    public void AddType_ValueType_ReturnsCorrectSchema(Type type, SchemaValueType expectedType)
    {
        // Arrange
        var builder = new JsonSchemaBuilder();

        // Act
        JsonSchema result = builder.AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(expectedType, result.GetJsonType());
    }

    [Theory]
    [InlineData(typeof(int), SchemaValueType.Integer, int.MinValue, int.MaxValue)]
    [InlineData(typeof(long), SchemaValueType.Integer, long.MinValue, long.MaxValue)]
    [InlineData(typeof(short), SchemaValueType.Integer, short.MinValue, short.MaxValue)]
    [InlineData(typeof(byte), SchemaValueType.Integer, byte.MinValue, byte.MaxValue)]
    [InlineData(typeof(sbyte), SchemaValueType.Integer, sbyte.MinValue, sbyte.MaxValue)]
    [InlineData(typeof(uint), SchemaValueType.Integer, uint.MinValue, uint.MaxValue)]
    [InlineData(typeof(ulong), SchemaValueType.Integer, ulong.MinValue, ulong.MaxValue)]
    [InlineData(typeof(ushort), SchemaValueType.Integer, ushort.MinValue, ushort.MaxValue)]
    public void AddType_NumericType_ReturnsCorrectSchema(Type type, SchemaValueType expectedType, decimal expectedMinimum, decimal expectedMaximum)
    {
        // Arrange
        var builder = new JsonSchemaBuilder();

        // Act
        JsonSchema result = builder.AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(expectedType, result.GetJsonType());
        Assert.Equal(expectedMinimum, result.GetMinimum());
        Assert.Equal(expectedMaximum, result.GetMaximum());
    }

    [Fact]
    public void AddType_Decimal_ReturnsCorrectSchema()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        Type type = typeof(decimal);
        SchemaValueType expectedType = SchemaValueType.Number;

        // Act
        JsonSchema result = builder.AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(expectedType, result.GetJsonType());
        Assert.Equal(decimal.MinValue, result.GetMinimum());
        Assert.Equal(decimal.MaxValue, result.GetMaximum());
    }

    [Theory]
    [MemberData(nameof(AddType_StringFormatType_ReturnsCorrectSchema_Data))]
    public void AddType_StringFormatType_ReturnsCorrectSchema(Type type, Format expectedFormat)
    {
        // Arrange
        var builder = new JsonSchemaBuilder();

        // Act
        JsonSchema result = builder.AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(SchemaValueType.String, result.GetJsonType());
        Assert.Equal(expectedFormat, result.GetFormat());
    }

    public static TheoryData<Type, Format> AddType_StringFormatType_ReturnsCorrectSchema_Data => new()
    {
        { typeof(DateTime), Formats.DateTime },
        { typeof(DateTimeOffset), Formats.DateTime },
        { typeof(DateOnly), Formats.Date },
        { typeof(TimeOnly), Formats.Time },
        { typeof(Guid), Formats.Uuid },
        { typeof(Uri), Formats.Uri },
    };

    [Fact]
    public void AddType_String_ReturnsCorrectSchema()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        Type type = typeof(string);
        SchemaValueType expectedType = SchemaValueType.String;

        // Act
        JsonSchema result = builder.AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(expectedType, result.GetJsonType());
    }

    [Fact]
    public void AddType_Object_ReturnsCorrectSchema()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        Type type = typeof(PropertyAnnotationTestObject);
        SchemaValueType expectedType = SchemaValueType.Object;

        // Act
        JsonSchema result = builder.AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(expectedType, result.GetJsonType());
    }

    [Theory]
    [InlineData(typeof(int[]), SchemaValueType.Integer)]
    [InlineData(typeof(long[]), SchemaValueType.Integer)]
    [InlineData(typeof(short[]), SchemaValueType.Integer)]
    [InlineData(typeof(byte[]), SchemaValueType.Integer)]
    [InlineData(typeof(sbyte[]), SchemaValueType.Integer)]
    [InlineData(typeof(uint[]), SchemaValueType.Integer)]
    [InlineData(typeof(ulong[]), SchemaValueType.Integer)]
    [InlineData(typeof(ushort[]), SchemaValueType.Integer)]
    [InlineData(typeof(float[]), SchemaValueType.Number)]
    [InlineData(typeof(double[]), SchemaValueType.Number)]
    [InlineData(typeof(decimal[]), SchemaValueType.Number)]
    [InlineData(typeof(string[]), SchemaValueType.String)]
    [InlineData(typeof(DateTime[]), SchemaValueType.String)]
    [InlineData(typeof(DateTimeOffset[]), SchemaValueType.String)]
    [InlineData(typeof(Guid[]), SchemaValueType.String)]
    [InlineData(typeof(Uri[]), SchemaValueType.String)]
    [InlineData(typeof(TestEnum[]), SchemaValueType.String)]
    [InlineData(typeof(IEnumerable<int>), SchemaValueType.Integer)]
    [InlineData(typeof(ICollection<int>), SchemaValueType.Integer)]
    [InlineData(typeof(IList<int>), SchemaValueType.Integer)]
    [InlineData(typeof(List<int>), SchemaValueType.Integer)]
    [InlineData(typeof(ImmutableList<int>), SchemaValueType.Integer)]
    [InlineData(typeof(IImmutableList<int>), SchemaValueType.Integer)]
    public void AddType_Array_ReturnsCorrectSchema(Type type, SchemaValueType expectedItemType)
    {
        // Arrange
        var builder = new JsonSchemaBuilder();

        // Act
        JsonSchema result = builder.AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(SchemaValueType.Array, result.GetJsonType());
        Assert.Equal(expectedItemType, result.GetItems()!.GetJsonType());
    }

    [Fact]
    public void AddType_NullableValueType_ReturnsUnderlyingSchema()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        Type type = typeof(int?);
        SchemaValueType expectedType = SchemaValueType.Integer;

        // Act
        JsonSchema result = builder.AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(expectedType, result.GetJsonType());
    }

    [Theory]
    [InlineData(typeof(Dictionary<string, int>))]
    [InlineData(typeof(ImmutableDictionary<string, int>))]
    [InlineData(typeof(IDictionary<string, int>))]
    [InlineData(typeof(IReadOnlyDictionary<string, int>))]
    [InlineData(typeof(IImmutableDictionary<string, int>))]
    public void AddType_DictionaryType_ReturnsObjectSchemaWithAdditionalProperties(Type type)
    {
        // Act
        JsonSchema result = new JsonSchemaBuilder().AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(SchemaValueType.Object, result.GetJsonType());
        JsonSchema? additionalProperties = result.GetAdditionalProperties();
        Assert.NotNull(additionalProperties);
        Assert.Equal(SchemaValueType.Integer, additionalProperties.GetJsonType());
    }

    [Fact]
    public void AddPropertyInfo_RequiredProperty_ReturnsRequiredSchema()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        System.Reflection.PropertyInfo? property = typeof(PropertyAnnotationTestObject).GetProperty("RequiredProperty");
        bool expectedIsRequired = true;

        // Act
        JsonSchema result = builder.AddPropertyInfo(property!, new ConverterContext(), out bool isRequired);
        this.OutputSchema(result);

        // Assert
        Assert.Equal(expectedIsRequired, isRequired);
    }

    [Fact]
    public void AddPropertyInfo_NullableProperty_ReturnsNullableSchema()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        System.Reflection.PropertyInfo? property = typeof(PropertyAnnotationTestObject).GetProperty("NullableProperty");
        bool expectedIsRequired = false;

        // Act
        JsonSchema result = builder.AddPropertyInfo(property!, new ConverterContext(), out bool isRequired);
        this.OutputSchema(result);

        // Assert
        Assert.Equal(expectedIsRequired, isRequired);
        Assert.Equal(2, result.GetOneOf()!.Count);
        Assert.Equal(SchemaValueType.Null, result.GetOneOf()!.ElementAt(1).GetJsonType());
    }

    [Fact]
    public void AddPropertyInfo_NonNullableProperty_ReturnsNonNullableSchema()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        System.Reflection.PropertyInfo? property = typeof(PropertyAnnotationTestObject).GetProperty("NonNullableProperty");
        bool expectedIsRequired = true;

        // Act
        JsonSchema result = builder.AddPropertyInfo(property!, new ConverterContext(), out bool isRequired);
        this.OutputSchema(result);

        // Assert
        Assert.Equal(expectedIsRequired, isRequired);
        Assert.Null(result.GetOneOf());
        Assert.Equal(SchemaValueType.Integer, result.GetJsonType());
    }

    [Fact]
    public void AddPropertyInfo_PropertyWithCustomAttributes_ReturnsSchemaWithAnnotations()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        System.Reflection.PropertyInfo? property = typeof(PropertyAnnotationTestObject).GetProperty("AnnotatedProperty");
        string expectedTitle = "Annotated Property";
        string expectedDescription = "This is an annotated property.";

        // Act
        JsonSchema result = builder.AddPropertyInfo(property!, new ConverterContext(), out _);
        this.OutputSchema(result);

        // Assert
        Assert.Equal(expectedTitle, result.GetTitle());
        Assert.Equal(expectedDescription, result.GetDescription());
    }

    [Fact]
    public void AddType_ComplexType_ReturnsCorrectSchema()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        Type type = typeof(ComplexObject);

        // Act
        JsonSchema result = builder.AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(SchemaValueType.Object, result.GetJsonType());

        IReadOnlyDictionary<string, JsonSchema>? properties = result.GetProperties();
        Assert.NotNull(properties);
        Assert.Equal(5, properties.Count);
        Assert.Equal(SchemaValueType.String, properties[nameof(ComplexObject.Name).ToJsonPropertyName()].GetJsonType());
        Assert.Equal(SchemaValueType.Integer, properties[nameof(ComplexObject.Age).ToJsonPropertyName()].GetJsonType());
        Assert.NotNull(properties[nameof(ComplexObject.ForceRequiredValue).ToJsonPropertyName()].GetOneOf());
        Assert.NotNull(properties["IncludedPrivateProperty".ToJsonPropertyName()].GetOneOf());
        Assert.NotNull(properties["ForceRequiredReference".ToJsonPropertyName()].GetOneOf());

        IReadOnlyList<string>? requiredProperties = result.GetRequired();
        Assert.NotNull(requiredProperties);
        Assert.Equal(4, requiredProperties.Count);
        Assert.Contains(nameof(ComplexObject.Name).ToJsonPropertyName(), requiredProperties);
        Assert.Contains(nameof(ComplexObject.Age).ToJsonPropertyName(), requiredProperties);
        Assert.Contains(nameof(ComplexObject.ForceRequiredValue).ToJsonPropertyName(), requiredProperties);
        Assert.Contains("ForceRequiredReference".ToJsonPropertyName(), requiredProperties);
    }

    [Fact]
    public void AddType_DictionaryWithNonStringKey_Throws()
    {
        Type type = typeof(Dictionary<int, int>);
        Assert.Throws<ArgumentException>(() => new JsonSchemaBuilder().AddType(type, new ConverterContext()));
    }

    [Fact]
    public void AddType_RecordClass_ReturnsCorrectSchema()
    {
        // Arrange
        Type type = typeof(TestRecord);

        // Act
        JsonSchema result = new JsonSchemaBuilder().AddType(type, new ConverterContext(), isRootType: true);
        this.OutputSchema(result);

        // Assert
        Assert.Equal(SchemaValueType.Object, result.GetJsonType());
        Assert.Contains("id", result.GetRequired()!);
        Assert.Contains("name", result.GetRequired()!);
    }

    [Fact]
    public void AddType_AbstractType_ReturnsObjectSchemaWithOneOfConcreteTypes()
    {
        // Arrange
        Type type = typeof(AbstractClass);

        // Act
        JsonSchema result = new JsonSchemaBuilder().AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(2, result.GetOneOf()!.Count);
    }

    [Fact]
    public void AddType_WithAmbientSchema_ReturnsObjectSchemaWithProperties()
    {
        // Arrange
        Type type = typeof(AmbientClass);

        // Act
        JsonSchema result = new JsonSchemaBuilder().AddType(type, new ConverterContext());
        this.OutputSchema(result);

        // Assert
        Assert.Equal(SchemaValueType.Object, result.GetJsonType());
        Assert.Contains("name", result.GetProperties()!);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Test Code")]
    private enum TestEnum
    {
        Value1,
        Value2,
        Value3,
    }

    private class PropertyAnnotationTestObject
    {
        [Display(Name = "Object Age", Description = "The age of the object")]
        public int? NullableProperty { get; set; }

        [Description("This is for a $comment property")]
        public int NonNullableProperty { get; set; }

        [Display(Name = "Annotated Property", Description = "This is an annotated property.")]
        public string AnnotatedProperty { get; set; } = string.Empty;

        [SchemaRequired]
        public string RequiredProperty { get; set; } = string.Empty;
    }

    [DisplayName("https://schema.org/object-with-properties")]
    private class ComplexObject
    {
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }

        [JsonRequired]
        public decimal? ForceRequiredValue { get; set; }

        // Properties with private getters are ignored
        public float Height { private get; set; }

        [JsonIgnore]
        public decimal IgnoredProperty { get; set; }

        [JsonInclude]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test Code")]
        private string? IncludedPrivateProperty { get; set; }

        [JsonRequired]
        [JsonInclude]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test Code")]
        private PropertyAnnotationTestObject? ForceRequiredReference { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test Code")]
        private string? PrivateProperty { get; set; }

        // Indexers are ignored
        public int this[int i] => 0;
    }

    private record TestRecord(int Id, string Name);

    private abstract class AbstractClass
    {
        public string Name { get; set; } = string.Empty;
    }

    private class ConcreteClass1 : AbstractClass
    {
        public int Age { get; set; }
    }

    private class ConcreteClass2 : AbstractClass
    {
        public float Height { get; set; }
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
}
