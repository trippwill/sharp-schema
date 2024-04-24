// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Json.Schema;

namespace SharpSchema;

/// <summary>
/// Represents the context for the SharpSchema library.
/// </summary>
public class ConverterContext
{
    /// <summary>
    /// Gets the definitions dictionary.
    /// </summary>
    public Dictionary<string, JsonSchemaBuilder> Defs { get; init; } = [];

    /// <summary>
    /// Gets the maximum depth for schema generation.
    /// </summary>
    public int MaxDepth { get; init; } = 50;

    /// <summary>
    /// Gets or sets the current depth for schema generation.
    /// </summary>
    public int CurrentDepth { get; set; } = 0;

    /// <summary>
    /// Gets a value indicating whether to include interfaces in schema generation.
    /// </summary>
    public bool IncludeInterfaces { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to use the underlying type of an enum in schema generation.
    /// </summary>
    public bool EnumAsUnderlyingType { get; init; } = false;
}
