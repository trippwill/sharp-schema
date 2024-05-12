// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Annotations;

/// <summary>
/// Overrides the schema.
/// </summary>
/// <remarks>
/// This attribute is used to specify a custom value for a schema.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
public class SchemaOverrideAttribute(string value) : Attribute
{
    /// <summary>
    /// Gets the overridden value for the schema.
    /// </summary>
    public string Value { get; } = value;
}
