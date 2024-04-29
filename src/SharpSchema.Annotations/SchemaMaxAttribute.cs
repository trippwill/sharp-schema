// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Annotations;

/// <summary>
/// Specifies a maximum value for a schema.
/// </summary>
/// <remarks>
/// This attribute is used to annotate types, properties or fields in a schema class
/// to indicate some maximum value allowed for the property or field.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property)]
public class SchemaMaxAttribute(int maximum) : SchemaAttribute
{
    /// <summary>
    /// Gets the minimum value specified by the attribute.
    /// </summary>
    public int Maximum { get; } = maximum;
}
