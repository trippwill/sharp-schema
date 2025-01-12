// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Json.Schema;
using Xunit;
using Xunit.Abstractions;
using SharpSchema.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace SharpSchema.Tests;

public class JsonSchemaBuilderExtensionsTests(ITestOutputHelper outputHelper) : TestBase(outputHelper)
{
    private static JsonSchema BuildSchema(Type type)
    {
        var builder = new JsonSchemaBuilder();
        return builder.AddType(new ConverterContext() { RootTypeAssemblyName = type.Assembly.GetName() }, RootTypeContext.FromType(type));
    }

    private static void AssertSchema(
        JsonSchema schema,
        SchemaValueType expectedType,
        decimal? expectedMinimum = null,
        decimal? expectedMaximum = null,
        Format? expectedFormat = null)
    {
        Assert.Equal(expectedType, schema.GetJsonType());
        if (expectedMinimum.HasValue)
        {
            Assert.Equal(expectedMinimum, schema.GetMinimum());
        }

        if (expectedMaximum.HasValue)
        {
            Assert.Equal(expectedMaximum, schema.GetMaximum());
        }

        if (expectedFormat is not null)
        {
            Assert.Equal(expectedFormat, schema.GetFormat());
        }
    }

    [Theory]
    [InlineData(typeof(bool), SchemaValueType.Boolean)]
    [InlineData(typeof(char), SchemaValueType.String, Skip = "Not currently supported")]
    public void AddType_ValueType_ReturnsCorrectSchema(Type type, SchemaValueType expectedType)
    {
        // Act
        JsonSchema result = BuildSchema(type);
        this.OutputSchema(result);

        // Assert
        AssertSchema(result, expectedType);
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
        // Act
        JsonSchema result = BuildSchema(type);
        this.OutputSchema(result);

        // Assert
        AssertSchema(result, expectedType, expectedMinimum, expectedMaximum);
    }

    [Fact]
    public void AddType_Decimal_ReturnsCorrectSchema()
    {
        // Act
        JsonSchema result = BuildSchema(typeof(decimal));
        this.OutputSchema(result);

        // Assert
        AssertSchema(result, SchemaValueType.Number, decimal.MinValue, decimal.MaxValue);
    }

    [Theory]
    [MemberData(nameof(AddType_StringFormatType_ReturnsCorrectSchema_Data))]
    public void AddType_StringFormatType_ReturnsCorrectSchema(Type type, Format expectedFormat)
    {
        // Act
        JsonSchema result = BuildSchema(type);
        this.OutputSchema(result);

        // Assert
        AssertSchema(result, SchemaValueType.String, expectedFormat: expectedFormat);
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
        // Act
        JsonSchema result = BuildSchema(typeof(string));
        this.OutputSchema(result);

        // Assert
        AssertSchema(result, SchemaValueType.String);
    }

    [Fact]
    public void AddType_Object_ReturnsCorrectSchema()
    {
        // Act
        JsonSchema result = BuildSchema(typeof(PropertyAnnotationTestObject));
        this.OutputSchema(result);

        // Assert
        AssertSchema(result, SchemaValueType.Object);
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
    [InlineData(typeof(IEnumerable<int>), SchemaValueType.Integer)]
    [InlineData(typeof(ICollection<int>), SchemaValueType.Integer)]
    [InlineData(typeof(IList<int>), SchemaValueType.Integer)]
    [InlineData(typeof(List<int>), SchemaValueType.Integer)]
    [InlineData(typeof(ImmutableList<int>), SchemaValueType.Integer)]
    [InlineData(typeof(IImmutableList<int>), SchemaValueType.Integer)]
    public void AddType_Array_ReturnsCorrectSchema(Type type, SchemaValueType expectedItemType)
    {
        // Act
        JsonSchema result = BuildSchema(type);
        this.OutputSchema(result);

        // Assert
        Assert.Equal(SchemaValueType.Array, result.GetJsonType());
        Assert.Equal(expectedItemType, result.GetItems()!.GetJsonType());
    }

    [Fact]
    public void AddType_NullableValueType_ReturnsUnderlyingSchema()
    {
        // Act
        JsonSchema result = BuildSchema(typeof(int?));
        this.OutputSchema(result);

        // Assert
        AssertSchema(result, SchemaValueType.Integer);
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
        JsonSchema result = BuildSchema(type);
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
        JsonSchema result = builder.AddPropertyInfo(new ConverterContext() { RootTypeAssemblyName = property!.PropertyType.Assembly.GetName()}, property!, out bool isRequired);
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
        JsonSchema result = builder.AddPropertyInfo(new ConverterContext() { RootTypeAssemblyName = property!.PropertyType.Assembly.GetName() }, property!, out bool isRequired);
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
        JsonSchema result = builder.AddPropertyInfo(new ConverterContext() { RootTypeAssemblyName = property!.PropertyType.Assembly.GetName() }, property!, out bool isRequired);
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
        JsonSchema result = builder.AddPropertyInfo(new ConverterContext() { RootTypeAssemblyName = property!.PropertyType.Assembly.GetName() }, property!, out _);
        this.OutputSchema(result);

        // Assert
        Assert.Equal(expectedTitle, result.GetTitle());
        Assert.Equal(expectedDescription, result.GetDescription());
    }

    [Fact]
    public void AddType_ComplexType_ReturnsCorrectSchema()
    {
        // Act
        JsonSchema result = BuildSchema(typeof(ComplexObject));
        this.OutputSchema(result);

        // Assert
        AssertSchema(result, SchemaValueType.Object);

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
    public void AddType_RecordClass_ReturnsCorrectSchema()
    {
        // Act
        JsonSchema result = BuildSchema(typeof(TestRecord));
        this.OutputSchema(result);

        // Assert
        AssertSchema(result, SchemaValueType.Object);
        Assert.Contains("id", result.GetRequired()!);
        Assert.Contains("name", result.GetRequired()!);
    }

    [Fact]
    public void AddType_AbstractType_ReturnsObjectSchemaWithOneOfConcreteTypes()
    {
        // Act
        JsonSchema result = BuildSchema(typeof(AbstractClass));
        this.OutputSchema(result);

        // Assert
        Assert.Equal(2, result.GetOneOf()!.Count);
    }

    [Fact]
    public void AddType_WithOverrideSchema_ReturnsRef()
    {
        // Act
        JsonSchema result = BuildSchema(typeof(OverrideClass));
        this.OutputSchema(result);

        // Assert
        Assert.NotNull(result.GetRef());
    }

    [Fact]
    public void AddTypeAnnotations_WithDocComments_ReturnsSchemaWithAnnotations()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        var context = new ConverterContext { ParseDocComments = true, RootTypeAssemblyName = typeof(PropertyAnnotationTestObject).Assembly.GetName() };
        Type type = typeof(PropertyAnnotationTestObject);

        // Act
        JsonSchema result = builder.AddType(context, RootTypeContext.FromType(type)).AddTypeAnnotations(context, type, disallowDocComments: false);
        this.OutputSchema(result);

        // Assert
        Assert.Equal("This is a test of the doc comment loader.", result.GetDescription());
    }

    [Fact]
    public void AddPropertyAnnotation_WithComplexComments_Works()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        var context = new ConverterContext { ParseDocComments = true, RootTypeAssemblyName = typeof(PropertyAnnotationTestObject).Assembly.GetName() };
        System.Reflection.PropertyInfo? property = typeof(PropertyAnnotationTestObject).GetProperty("PropertyWithLotsOfTags");
        // Act
        JsonSchema result = builder.AddPropertyInfo(context, property!, out _);
        this.OutputSchema(result);

        Assert.Contains("This is the first paragraph.", result.GetDescription());
        Assert.Contains("Gets or sets the property with lots of tags.", result.GetTitle());
        Assert.NotEmpty(result.GetExamples()!);
    }

    [Fact]
    public void AddTypeAnnotations_WithoutDocComments_ReturnsSchemaWithoutAnnotations()
    {
        // Arrange
        var builder = new JsonSchemaBuilder();
        var context = new ConverterContext { ParseDocComments = false, RootTypeAssemblyName = typeof(PropertyAnnotationTestObject).Assembly.GetName() };
        Type type = typeof(PropertyAnnotationTestObject);

        // Act
        JsonSchema result = builder.AddTypeAnnotations(context, type, disallowDocComments: true);
        this.OutputSchema(result);

        // Assert
        Assert.Null(result.GetTitle());
        Assert.Null(result.GetDescription());
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Test Code")]
    private enum TestEnum
    {
        Value1,
        Value2,
        Value3,
    }

    /// <remarks>
    /// This is a test of the doc comment loader.
    /// </remarks>
    private class PropertyAnnotationTestObject
    {
        /// <summary>
        /// Gets or sets the age of the object.
        /// </summary>
        /// <remarks>
        /// This is a test of the doc comment loader.
        /// </remarks>
        [SchemaMeta(Title = "Object Age")]
        public int? NullableProperty { get; set; }

        [SchemaMeta(Comment = "This is for a $comment property")]
        public int NonNullableProperty { get; set; }

        [SchemaMeta(Title = "Annotated Property", Description = "This is an annotated property.")]
        public string AnnotatedProperty { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the required property.
        /// </summary>
        [SchemaRequired, SchemaMeta]
        public string RequiredProperty { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the property with lots of tags.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is the first paragraph. <br />
        ///         It <see cref="int"> references a type</see>
        ///     </para>
        /// </remarks>
        /// <example>"propertyWithLotsOfTags" = 76</example>
        public int PropertyWithLotsOfTags { get; set; }
    }

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
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test Code")]
        private string? IncludedPrivateProperty { get; set; }

        [JsonRequired]
        [JsonInclude]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test Code")]
        private PropertyAnnotationTestObject? ForceRequiredReference { get; set; }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test Code")]
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

    [SchemaOverride(Schema)]
    private class OverrideClass
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
