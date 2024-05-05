// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Humanizer;

namespace SharpSchema;

/// <summary>
/// Provides extension methods for <see cref="string"/> objects.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts the string value to a JSON property name by applying camel case formatting.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>The converted JSON property name.</returns>
    public static string ToJsonPropertyName(this string value) => value.Camelize();
}
