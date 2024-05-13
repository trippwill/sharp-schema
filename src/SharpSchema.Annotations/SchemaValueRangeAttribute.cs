﻿// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Annotations;

/// <summary>
/// Represents an attribute that specifies a value range for a schema.
/// </summary>
/// <remarks>
/// This attribute can be used to define the minimum and maximum values allowed for a property in a schema.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SchemaValueRangeAttribute : SchemaAttribute
{
    /// <summary>
    /// Gets or sets the minimum value allowed for the property.
    /// </summary>
    public double Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum value allowed for the property.
    /// </summary>
    public double Max { get; set; }
}