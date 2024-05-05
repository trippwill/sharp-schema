// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Annotations;

/// <summary>
/// Represents an attribute that specifies the length range for a schema.
/// </summary>
/// <remarks>
/// This attribute can be used to define the minimum and maximum length of a schema.
/// </remarks>
public class SchemaLengthRangeAttribute(uint minLength = 0, uint maxLength = uint.MaxValue) : SchemaAttribute
{
    /// <summary>
    /// Gets the minimum length allowed for the schema.
    /// </summary>
    public uint MinLength { get; } = minLength;

    /// <summary>
    /// Gets the maximum length allowed for the schema.
    /// </summary>
    public uint MaxLength { get; } = maxLength;
}
