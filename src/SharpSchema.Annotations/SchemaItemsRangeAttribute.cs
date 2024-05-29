// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Represents an attribute that specifies the items range for an schema.
/// </summary>
/// <remarks>
/// This attribute can be used to define the minimum and maximum items of an array schema, and to set the uniqueItems constraint.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaItemsRangeAttribute : SchemaAttribute
#else
internal class SchemaItemsRangeAttribute : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets or sets the minimum items allowed for the array.
    /// </summary>
    public uint Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum items allowed for the array.
    /// </summary>
    public uint Max { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the items in the array must be unique.
    /// </summary>
    public bool UniqueItems { get; set; }
}
