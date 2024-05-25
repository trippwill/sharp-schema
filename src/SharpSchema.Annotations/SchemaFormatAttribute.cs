// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Annotations;

/// <summary>
/// Applies a format to a schema property.
/// </summary>
/// <remarks>
/// This attribute is used to specify the format of a schema.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
#if ASSEMBLY
public class SchemaFormatAttribute(string format) : SchemaAttribute
#else
internal class SchemaFormatAttribute(string format) : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets the format of the schema.
    /// </summary>
    public string Format { get; } = format;
}
