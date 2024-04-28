// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema;

/// <summary>
/// Information about a schema root type.
/// </summary>
public record RootTypeContext(Type Type, string? Filename, string? Id, string? CommonNamespace);
