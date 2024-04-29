// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Annotations;

/// <summary>
/// Specifies a minimum value for a schema.
/// </summary>
/// <remarks>
/// This attribute is used to annotate types, properties or fields in a schema class
/// to indicate some minimum value allowed for the property or field.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property)]
public class SchemaMinAttribute(int minimum) : SchemaAttribute
{
    /// <summary>
    /// Gets the minimum value specified by the attribute.
    /// </summary>
    public int Minimum { get; } = minimum;
}
