// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Json.More;
using Json.Schema;
using Xunit.Abstractions;

namespace SharpSchema.Tests;

public class TestBase(ITestOutputHelper outputHelper)
{
    private readonly JsonSerializerOptions writeOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    protected ITestOutputHelper Output { get; } = outputHelper;

    protected void OutputSchema(JsonSchema schema)
    {
        this.Output.WriteLine(
            JsonSerializer.Serialize(
                schema.ToJsonDocument().RootElement,
                this.writeOptions));
    }
}
