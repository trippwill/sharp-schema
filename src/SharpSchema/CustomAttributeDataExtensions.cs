// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft;

namespace SharpSchema;

/// <summary>
/// Provides extension methods for <see cref="CustomAttributeData"/> instances.
/// </summary>
public static class CustomAttributeDataExtensions
{
    /// <summary>
    /// Retrieves the value of a named argument from the specified <see cref="CustomAttributeData"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of the argument value.</typeparam>
    /// <param name="customAttributeData">The <see cref="CustomAttributeData"/> instance.</param>
    /// <param name="name">The name of the argument.</param>
    /// <returns>The value of the named argument, or the default value of type <typeparamref name="T"/> if the argument is not found.</returns>
    public static T? GetNamedArgument<T>(this CustomAttributeData customAttributeData, string name)
    {
        Requires.NotNull(customAttributeData, nameof(customAttributeData));

        return customAttributeData
            .NamedArguments
            .FirstOrDefault(a => a.MemberName == name)
            .TypedValue.Value is T value ? value : default;
    }

    /// <summary>
    /// Retrieves the value of a constructor argument from the specified <see cref="CustomAttributeData"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of the argument value.</typeparam>
    /// <param name="customAttributeData">The <see cref="CustomAttributeData"/> instance.</param>
    /// <param name="index">The index of the constructor argument.</param>
    /// <returns>The value of the constructor argument, or the default value of type <typeparamref name="T"/> if the argument is not found.</returns>
    public static T? GetConstructorArgument<T>(this CustomAttributeData customAttributeData, int index)
    {
        Requires.NotNull(customAttributeData, nameof(customAttributeData));

        return customAttributeData
            .ConstructorArguments
            .ElementAtOrDefault(index)
            .Value is T value ? value : default;
    }
}
