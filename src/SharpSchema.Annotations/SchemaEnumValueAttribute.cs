// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Annotations;

/// <summary>
/// Specifies the value of an enum member in a schema.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class SchemaEnumValueAttribute(string value) : Attribute
{
    /// <summary>
    /// Gets the value of the enum member.
    /// </summary>
    public string Value { get; } = value;
}
