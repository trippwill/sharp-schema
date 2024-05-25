// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Annotations;

/// <summary>
/// Represents a range of properties allowed in a schema.
/// </summary>
/// <remarks>
/// This attribute can be used to specify the minimum and maximum number of properties allowed in a schema.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field)]
#if ASSEMBLY
public class SchemaPropertiesRangeAttribute : SchemaAttribute
#else
internal class SchemaPropertiesRangeAttribute : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets or sets the minimum number of properties allowed in a schema.
    /// </summary>
    public uint Min { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of properties allowed in a schema.
    /// </summary>
    public uint Max { get; set; }
}
