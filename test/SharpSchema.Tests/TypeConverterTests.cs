// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using Xunit;

namespace SharpSchema.Tests;

public class TypeConverterTests
{
    [Fact]
    public void Convert_ShouldSetSchemaToDraft07()
    {
        // Arrange
        RootTypeContext typeContext = new RootTypeContext { Type = typeof(SchemaRoot) };
        var converter = new TypeConverter(new TypeConverter.Options());

        // Act
        JsonSchema result = converter.Convert(typeContext);

        // Assert
        Assert.Equal("http://json-schema.org/draft-07/schema#", result.GetSchema()!.ToString());
    }

    [Fact]
    public void Convert_ShouldSetIdIfIdIsNotNull()
    {
        // Arrange
        var typeContext = new RootTypeContext { Type = typeof(SchemaRoot), Id = "http://example.com/schema" };
        var converter = new TypeConverter(new TypeConverter.Options());

        // Act
        JsonSchema result = converter.Convert(typeContext);

        // Assert
        Assert.Equal("http://example.com/schema", result.GetId()!.ToString());
    }

    [Fact]
    public void Convert_ShouldNotSetIdIfIdIsNull()
    {
        // Arrange
        RootTypeContext typeContext = new RootTypeContext(typeof(SchemaRoot), null, null, null);
        var converter = new TypeConverter(new TypeConverter.Options());

        // Act
        JsonSchema result = converter.Convert(typeContext);

        // Assert
        Assert.Null(result.GetId());
    }

    [Fact]
    public void Convert_Throws_WhenMaxDepthIsExceeded()
    {
        var typeContext = new RootTypeContext { Type = typeof(ExceedingMaxDepthClass) };

        Assert.Throws<InvalidOperationException>(() => new TypeConverter(new TypeConverter.Options()).Convert(typeContext));
    }

    [Theory]
    [MemberData(nameof(SchemaEvaluate_WithInput_ReturnsExpectedResult_Data))]
    public void Evaluate_ConvertedSchema_ReturnsExpectedResult(string caseName, string input, bool expectedIsValid)
    {
        _ = caseName;

        // Arrange
        JsonSchema schema = new TypeConverter(new TypeConverter.Options(expectedIsValid)).Convert(RootTypeContext.FromType(typeof(SchemaRoot)));

        // Act
        EvaluationResults results = schema.Evaluate(JsonDocument.Parse(input).RootElement, new EvaluationOptions()
        {
            OutputFormat = OutputFormat.List,
        });

        // Assert
        Assert.Equal(expectedIsValid, results.IsValid);
    }

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Test Code")]
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

    private class SchemaRoot
    {
        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }

        // Properties with private getters are ignored
        public float Height { private get; set; }

        [JsonIgnore]
        public decimal IgnoredProperty { get; set; }

        [JsonInclude]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Test Code")]
        private string? IncludedPrivateProperty { get; set; }
    }

    private class ExceedingMaxDepthClass
    {
        public ExceedingMaxDepthClass? Nested { get; set; }
    }
}
