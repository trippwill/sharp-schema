// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Marks a class or struct as a schema root.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaRootAttribute : SchemaAttribute
#else
internal class SchemaRootAttribute : SchemaAttribute
#endif
{
    /// <summary>
    /// Gets or sets the file name of the schema.
    /// </summary>
    public string? Filename { get; set; }

    /// <summary>
    /// Gets or sets the $id property of the schema.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the common namespace for the schema. Generated sub-schema names are derived
    /// from the namespace and name of the associated type. When not <see langword="null"/>, the
    /// <see cref="CommonNamespace"/> is trimmed from the start of the sub-schema name.
    /// </summary>
    public string? CommonNamespace { get; set; }
}
