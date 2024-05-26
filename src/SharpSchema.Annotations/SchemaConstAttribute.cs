// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Specifies a constant value for a property or field in a schema.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaConstAttribute(object value) : SchemaAttribute
#else
internal class SchemaConstAttribute(object value) : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets the constant value.
    /// </summary>
    public object Value { get; } = value;
}
