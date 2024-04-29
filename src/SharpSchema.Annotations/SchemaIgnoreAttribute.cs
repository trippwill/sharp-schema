// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Annotations;

/// <summary>
/// Specifies that the annotated element should be ignored during schema generation.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Struct)]
public class SchemaIgnoreAttribute : Attribute
{
}
