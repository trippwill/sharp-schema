// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Json.More;
using Json.Schema;

namespace SharpSchema;

/// <summary>
/// Extension methods for <see cref="JsonSchema"/>.
/// </summary>
public static class JsonSchemaExtensions
{
    /// <summary>
    /// Serializes a <see cref="JsonSchema"/> to a UTF-8 encoded byte array.
    /// </summary>
    /// <param name="schema">The schema to serialize.</param>
    /// <param name="options">The serializer options. If not provided, the formatting is indented by default.</param>
    /// <returns>The array of bytes.</returns>
    [ExcludeFromCodeCoverage]
    public static byte[] SerializeToUtf8Bytes(this JsonSchema schema, JsonSerializerOptions? options = null)
    {
        options ??= new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        return JsonSerializer.SerializeToUtf8Bytes(
            schema.ToJsonDocument().RootElement,
            options);
    }

    /// <summary>
    /// Serializes a <see cref="JsonSchema"/> to a JSON string.
    /// </summary>
    /// <param name="schema">The schema to serialize.</param>
    /// <param name="options">The serializer options. If not provided, the formatting is indented by default.</param>
    /// <returns>The JSON string.</returns>
    public static string SerializeToJson(this JsonSchema schema, JsonSerializerOptions? options = null)
    {
        options ??= new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        return JsonSerializer.Serialize(
            schema.ToJsonDocument().RootElement,
            options);
    }
}
