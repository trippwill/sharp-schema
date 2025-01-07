// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Json.More;
using Json.Schema;
using SharpSchema;
using Xunit;
using Xunit.Abstractions;

namespace Scenarios.Interface;

public class InterfaceTests(ITestOutputHelper outputHelper)
{
    [Theory(Skip = "This scenario does not currently work.")]
    [InlineData(true, TrueExpectedSchema)]
    [InlineData(false, FalseExpectedSchema)]
    public void Convert_ExpectedSchema(bool includeInterfaces, string expected)
    {
        RootTypeContext typeContext = RootTypeContext.FromType<WheelsCollection>() with
        {
            Id = "https://libanvl/test/scenario/interface",
            CommonNamespace = "Scenarios.Interface",
        };

        JsonSchema schema = new TypeConverter(new() { IncludeInterfaces = includeInterfaces }).Convert(typeContext);
        string schemaString = JsonSerializer.Serialize(
            schema.ToJsonDocument().RootElement,
            new JsonSerializerOptions { WriteIndented = true });

        outputHelper.WriteLine(schemaString);

        Assert.Equal(expected, schemaString);
    }

    private const string TrueExpectedSchema = /*lang=json*/ """

        """;

    private const string FalseExpectedSchema = /*lang=json*/ """

        """;
}
