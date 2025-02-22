﻿// Copyright (c) Charles Willis. All rights reserved.
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

    /// <summary>
    /// Converts the string value to a JSON property title by applying title case formatting.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>The converted JSON property title.</returns>
    public static string ToJsonPropertyTitle(this string value) => value.Titleize();

    /// <summary>
    /// Converts the string value to a JSON definition location by appending it to the JSON pointer for definitions.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>The converted JSON definition location.</returns>
    public static Uri ToJsonDefUri(this string value) => new($"#/$defs/{value}", UriKind.RelativeOrAbsolute);

    /// <summary>
    /// Converts "&lt;para&gt;" to double new-line, and "&lt;br/&gt;" to single new-line.
    /// </summary>
    /// <param name="value">The doc string.</param>
    /// <returns>The parsed doc string.</returns>
    public static string? ParseDocString(this string? value)
    {
        return string.IsNullOrEmpty(value)
            ? value
            : new System.Text.StringBuilder(value)
                .Replace("<para>", string.Empty)
                .Replace("</para>", "\n\n")
                .Replace("<br/>", "\n")
                .Replace("<br />", "\n")
                .ToString();
    }
}
