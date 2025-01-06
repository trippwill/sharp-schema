// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Json.More;
using Json.Schema;
using Xunit;
using Xunit.Abstractions;

namespace SharpSchema.Tests;

public class ExampleTests(ITestOutputHelper outputHelper) : TestBase(outputHelper)
{
    [Fact]
    public void SimplePerson()
    {
        var typeContext = RootTypeContext.FromType<Examples.SimplePerson>();
        JsonSchema schema = new TypeConverter().Convert(typeContext);
        this.OutputSchema(schema);

        Assert.True(true);
    }

    [Fact]
    public void SimpleOffice()
    {
        RootTypeContext typeContext = RootTypeContext.FromType<Examples.SimpleOffice>() with { CommonNamespace = "SharpSchema.Tests" };
        JsonSchema schema = new TypeConverter(new TypeConverter.Options
        {
            ParseDocComments = true,
        })
        .Convert(typeContext);

        this.OutputSchema(schema);

        // Test the schema meta override for title.
        Assert.Equal("Simple Office", schema.GetTitle());

        // Test the doc comment for description.
        Assert.Equal("An office is a collection of employees.", schema.GetDescription());
    }

    [Fact]
    public void GenericTypeHandling()
    {
        var typeContext = RootTypeContext.FromType<Examples.GenericType<int>>();
        JsonSchema schema = new TypeConverter().Convert(typeContext);
        this.OutputSchema(schema);

        Assert.NotNull(schema);
        Assert.Contains("oneOf", schema.ToJsonDocument().RootElement.ToJsonString());
        Assert.Contains("value", schema.GetDefs()?["SharpSchema.Tests.Examples_GenericType{System.Int32}"].GetRequired() ?? []);
    }

    [Fact]
    public void AbstractTypeHandling()
    {
        var typeContext = RootTypeContext.FromType<Examples.AbstractBase>();
        JsonSchema schema = new TypeConverter().Convert(typeContext);
        this.OutputSchema(schema);

        Assert.NotNull(schema);
        Assert.Contains("oneOf", schema.ToJsonDocument().RootElement.ToJsonString());
    }

    [Fact]
    public void CustomAttributeHandling()
    {
        var typeContext = RootTypeContext.FromType<Examples.CustomAttributeClass>();
        JsonSchema schema = new TypeConverter().Convert(typeContext);
        this.OutputSchema(schema);

        Assert.NotNull(schema);
        Assert.Contains("minProperties", schema.ToJsonDocument().RootElement.ToJsonString());
        Assert.Contains("maxProperties", schema.ToJsonDocument().RootElement.ToJsonString());
        Assert.DoesNotContain("property1", schema.GetRequired() ?? []);
    }
}
