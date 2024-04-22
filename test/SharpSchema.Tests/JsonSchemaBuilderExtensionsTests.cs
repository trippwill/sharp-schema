// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Json.Schema;
using Xunit;

namespace SharpSchema.Tests
{
    public class JsonSchemaBuilderExtensionsTests
    {
        [Fact]
        public void AddType_Enum_ReturnsStringSchemaWithEnumValues()
        {
            // Arrange
            var builder = new JsonSchemaBuilder();
            var defs = new Dictionary<string, JsonSchemaBuilder>();
            Type type = typeof(TestEnum);
            string[] expectedEnumValues = new[] { "value1", "value2", "value3" };

            // Act
            JsonSchema result = builder.AddType(type, defs);

            // Assert
            Assert.Equal(SchemaValueType.String, result.GetJsonType());
            Assert.Equal(expectedEnumValues, result.GetEnum()!.Select(jn => jn!.ToString()));
        }

        [Fact]
        public void AddType_ValueType_ReturnsCorrectSchema()
        {
            // Arrange
            var builder = new JsonSchemaBuilder();
            var defs = new Dictionary<string, JsonSchemaBuilder>();
            Type type = typeof(int);
            SchemaValueType expectedType = SchemaValueType.Integer;

            // Act
            JsonSchema result = builder.AddType(type, defs);

            // Assert
            Assert.Equal(expectedType, result.GetJsonType());
        }

        [Fact]
        public void AddType_String_ReturnsCorrectSchema()
        {
            // Arrange
            var builder = new JsonSchemaBuilder();
            var defs = new Dictionary<string, JsonSchemaBuilder>();
            Type type = typeof(string);
            SchemaValueType expectedType = SchemaValueType.String;

            // Act
            JsonSchema result = builder.AddType(type, defs);

            // Assert
            Assert.Equal(expectedType, result.GetJsonType());
        }

        [Fact]
        public void AddType_Object_ReturnsCorrectSchema()
        {
            // Arrange
            var builder = new JsonSchemaBuilder();
            var defs = new Dictionary<string, JsonSchemaBuilder>();
            Type type = typeof(TestObject);
            SchemaValueType expectedType = SchemaValueType.Object;

            // Act
            JsonSchema result = builder.AddType(type, defs, isTopLevel: true);

            // Assert
            Assert.Equal(expectedType, result.GetJsonType());
        }

        [Fact]
        public void AddPropertyInfo_RequiredProperty_ReturnsRequiredSchema()
        {
            // Arrange
            var builder = new JsonSchemaBuilder();
            var defs = new Dictionary<string, JsonSchemaBuilder>();
            System.Reflection.PropertyInfo? property = typeof(TestObject).GetProperty("RequiredProperty");
            bool expectedIsRequired = true;

            // Act
            JsonSchemaBuilder result = builder.AddPropertyInfo(property!, defs, 0, out bool isRequired);

            // Assert
            Assert.Equal(expectedIsRequired, isRequired);
        }

        [Fact]
        public void AddPropertyInfo_NullableProperty_ReturnsNullableSchema()
        {
            // Arrange
            var builder = new JsonSchemaBuilder();
            var defs = new Dictionary<string, JsonSchemaBuilder>();
            System.Reflection.PropertyInfo? property = typeof(TestObject).GetProperty("NullableProperty");
            bool expectedIsRequired = false;

            // Act
            JsonSchema result = builder.AddPropertyInfo(property!, defs, 0, out bool isRequired);

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
            var defs = new Dictionary<string, JsonSchemaBuilder>();
            System.Reflection.PropertyInfo? property = typeof(TestObject).GetProperty("NonNullableProperty");
            bool expectedIsRequired = true;

            // Act
            JsonSchema result = builder.AddPropertyInfo(property!, defs, 0, out bool isRequired);

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
            var defs = new Dictionary<string, JsonSchemaBuilder>();
            System.Reflection.PropertyInfo? property = typeof(TestObject).GetProperty("AnnotatedProperty");
            string expectedTitle = "Annotated Property";
            string expectedDescription = "This is an annotated property.";

            // Act
            JsonSchema result = builder.AddPropertyInfo(property!, defs, 0, out _);

            // Assert
            Assert.Equal(expectedTitle, result.GetTitle());
            Assert.Equal(expectedDescription, result.GetDescription());
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "Test Code")]
        private enum TestEnum
        {
            Value1,
            Value2,
            Value3,
        }

        private class TestObject
        {
            public int? NullableProperty { get; set; }

            public int NonNullableProperty { get; set; }

            [System.ComponentModel.DataAnnotations.Display(Name = "Annotated Property", Description = "This is an annotated property.")]
            public string AnnotatedProperty { get; set; } = string.Empty;

            [System.ComponentModel.DataAnnotations.Required]
            public string RequiredProperty { get; set; } = string.Empty;
        }
    }
}
