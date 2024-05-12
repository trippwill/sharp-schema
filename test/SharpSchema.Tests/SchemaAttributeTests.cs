// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Json.Schema;
using SharpSchema.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace SharpSchema.Tests;

public class SchemaAttributeTests(ITestOutputHelper outputHelper) : TestBase(outputHelper)
{
    private const uint SchemaMinPropertiesValue = 1;
    private const uint SchemaMaxPropertiesValue = 10;

    [Fact]
    public void SchemaPropertiesRange_Applies_ToObjectPropertySchema()
    {
        TypeConverter converter = new();

        JsonSchema schema = converter.Convert(RootTypeContext.FromType<MinMaxParentObject>());
        this.OutputSchema(schema);

        JsonSchema? defSchema = schema.GetDefs()?.Values.ElementAt(1);
        Assert.NotNull(defSchema);
        Assert.Equal(SchemaMinPropertiesValue, defSchema.GetMinProperties());
        Assert.Equal(SchemaMaxPropertiesValue, defSchema.GetMaxProperties());
    }

    [Fact]
    public void SchemaPropertiesRange_Applies_ToObjectSchema()
    {
        TypeConverter converter = new();

        JsonSchema schema = converter.Convert(RootTypeContext.FromType<MinMaxObject>());
        this.OutputSchema(schema);

        Assert.Equal(SchemaMinPropertiesValue, schema.GetMinProperties());
        Assert.Equal(SchemaMaxPropertiesValue, schema.GetMaxProperties());
    }

    [Fact]
    public void SchemaRequired_AppliesRequiredProperties_ToObjectSchema()
    {
        TypeConverter converter = new();

        JsonSchema schema = converter.Convert(RootTypeContext.FromType<RequiredObject>());
        this.OutputSchema(schema);

        IReadOnlyList<string>? requiredProperties = schema.GetRequired();
        Assert.NotNull(requiredProperties);
        Assert.Contains("TestProperty".ToJsonPropertyName(), requiredProperties);
        Assert.DoesNotContain("TestProperty2".ToJsonPropertyName(), requiredProperties);
    }

    [Fact]
    public void SchemaPropertiesRange_Applies_ToPropertySchema()
    {
        TypeConverter converter = new();

        JsonSchema schema = converter.Convert(RootTypeContext.FromType<MinMaxObject>());
        this.OutputSchema(schema);

        JsonSchema? propertySchema = schema.GetProperties()?.Values.First();
        Assert.NotNull(propertySchema);
        Assert.Equal(SchemaMinPropertiesValue, propertySchema.GetMinProperties());
        Assert.Equal(SchemaMaxPropertiesValue, propertySchema.GetMaxProperties());
    }

    [Fact]
    public void SchemaIgnore_DoesNotIncludeIgnoredProperties_InObjectSchema()
    {
        TypeConverter converter = new();

        JsonSchema schema = converter.Convert(RootTypeContext.FromType<IgnoreObject>());
        this.OutputSchema(schema);

        IReadOnlyList<string>? requiredProperties = schema.GetRequired();
        Assert.NotNull(requiredProperties);
        Assert.DoesNotContain("TestProperty2".ToJsonPropertyName(), requiredProperties);
    }

    [Fact]
    public void SchemaValueRange_Applies_ToPropertySchema()
    {
        TypeConverter converter = new();

        JsonSchema schema = converter.Convert(RootTypeContext.FromType<ValueRangeObject>());
        this.OutputSchema(schema);

        JsonSchema? propertySchema = schema.GetProperties()?.Values.First();
        Assert.NotNull(propertySchema);
        Assert.Equal(1, propertySchema.GetMinimum());
        Assert.Equal(10, propertySchema.GetMaximum());
    }

    [Fact]
    public void SchemaLengthRange_Applies_ToPropertySchema()
    {
        TypeConverter converter = new();

        JsonSchema schema = converter.Convert(RootTypeContext.FromType<ValueRangeObject>());
        this.OutputSchema(schema);

        JsonSchema? propertySchema = schema.GetProperties()?.Values.ElementAt(1);
        Assert.NotNull(propertySchema);
        Assert.Equal(1U, propertySchema.GetMinLength());
        Assert.Equal(10U, propertySchema.GetMaxLength());
    }

    [SchemaPropertiesRange(Min = SchemaMinPropertiesValue, Max = SchemaMaxPropertiesValue)]
    private class MinMaxObject
    {
        [SchemaPropertiesRange(Min = SchemaMinPropertiesValue, Max = SchemaMaxPropertiesValue)]
        public required IgnoreObject TestProperty { get; init; }
    }

    private class MinMaxParentObject
    {
        public required MinMaxObject TestProperty { get; init; }
    }

    private class RequiredObject
    {
        [SchemaRequired]
        public int? TestProperty { get; }

        [SchemaRequired(false)]
        public int TestProperty2 { get; }
    }

    private class IgnoreObject
    {
        public int TestProperty { get; }

        [SchemaIgnore]
        public int TestProperty2 { get; }
    }

    private class ValueRangeObject
    {
        [SchemaValueRange(Min = 1, Max = 10)]
        public int TestProperty { get; }

        [SchemaLengthRange(Min = 1, Max = 10)]
        public required string TestProperty2 { get; init; }
    }
}
