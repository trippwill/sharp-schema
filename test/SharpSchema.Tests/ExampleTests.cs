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
        var typeContext = RootTypeContext.FromType(typeof(Examples.SimplePerson));
        TypeConverter converter = new();
        JsonSchema schema = converter.Convert(typeContext);
        this.OutputSchema(schema);

        Assert.True(true);
    }
}
