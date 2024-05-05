// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Annotations;

/// <summary>
/// Represents a range of properties allowed in a schema.
/// </summary>
/// <remarks>
/// This attribute can be used to specify the minimum and maximum number of properties allowed in a schema.
/// </remarks>
public class SchemaPropertiesRangeAttribute(uint minProperties = 0, uint maxProperties = uint.MaxValue) : SchemaAttribute
{
    /// <summary>
    /// Gets the minimum number of properties allowed in a schema.
    /// </summary>
    public uint MinProperties { get; } = minProperties;

    /// <summary>
    /// Gets the maximum number of properties allowed in a schema.
    /// </summary>
    public uint MaxProperties { get; } = maxProperties;
}
