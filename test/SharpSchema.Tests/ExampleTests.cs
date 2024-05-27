// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        JsonSchema schema = new TypeConverter().Convert(typeContext);
        this.OutputSchema(schema);

        Assert.True(true);
    }
}
