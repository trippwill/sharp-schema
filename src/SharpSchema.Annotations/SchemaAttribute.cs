// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Represents a base class for schema attributes.
/// </summary>
#if SHARPSCHEMA_ASSEMBLY
public abstract class SchemaAttribute : Attribute
#else
internal abstract class SchemaAttribute : Attribute
#endif
{
}
