// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Annotations;

/// <summary>
/// Indicates whether a property is required in a schema.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class SchemaRequiredAttribute(bool isRequired = true) : SchemaAttribute
{
    /// <summary>
    /// Gets a value indicating whether the property is required.
    /// </summary>
    public bool IsRequired { get; } = isRequired;
}
