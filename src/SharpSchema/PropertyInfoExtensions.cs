// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Serialization;
using Humanizer;
using SharpSchema.Annotations;

namespace SharpSchema;

/// <summary>
/// Extension methods for <see cref="PropertyInfo"/>.
/// </summary>
internal static class PropertyInfoExtensions
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    /// <summary>
    /// Determines whether the specified property should be skipped.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns><see langword="true"/> if the property should be skipped; otherwise, <see langword="false"/>.</returns>
    public static bool ShouldSkipProperty(this PropertyInfo property)
    {
        // skip indexers
        if (property.GetIndexParameters().Length > 0)
        {
            return true;
        }

        if (property.TryGetCustomAttributeData(typeof(JsonIgnoreAttribute), out CustomAttributeData? ignoreAttributeData))
        {
            int condition = ignoreAttributeData.GetNamedArgument<int>("Condition");
            return condition == default ? true : condition != (int)JsonIgnoreCondition.Never;
        }

        return false;
    }

    /// <summary>
    /// Gets the name of the specified property.
    /// </summary>
    /// <param name="property">The property to get the name of.</param>
    /// <returns>The name of the property.</returns>
    public static string GetPropertyName(this PropertyInfo property)
    {
        string name = property.Name;

        if (property.TryGetCustomAttributeData(typeof(JsonPropertyNameAttribute), out CustomAttributeData? cad))
        {
            name = cad.GetNamedArgument<string>("Name") ?? name;
        }

        return name.Camelize();
    }

    /// <summary>
    /// Determines whether the specified property is required.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <param name="isNullable">Indicates whether the property is nullable.</param>
    /// <returns><see langword="true"/> if the property is required; otherwise, <see langword="false"/>.</returns>
    public static bool IsRequired(this PropertyInfo property, out bool isNullable)
    {
        isNullable = property.IsNullable();

        if (property.TryGetCustomAttributeData(typeof(SchemaRequiredAttribute), out CustomAttributeData? cad))
        {
            return cad.GetConstructorArgument<bool>(0);
        }

        bool isJsonRequired = property.TryGetCustomAttributeData(typeof(JsonRequiredAttribute), out _);

        return isJsonRequired || !isNullable;
    }

    /// <summary>
    /// Determines whether the specified property is nullable.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns><see langword="true"/> if the property is nullable; otherwise, <see langword="false"/>.</returns>
    public static bool IsNullable(this PropertyInfo property)
    {
        if (property.PropertyType.IsValueType &&
            property.PropertyType.IsGenericType &&
            property.PropertyType.GetGenericTypeDefinition().Name == typeof(Nullable<>).Name)
        {
            return true;
        }
        else if (property.IsNullableReference())
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the specified property is a nullable reference type.
    /// </summary>
    /// <param name="property">The property to check.</param>
    /// <returns><c><see langword="true"/></c> if the property is a nullable reference type; otherwise, <see langword="false"/>.</returns>
    public static bool IsNullableReference(this PropertyInfo property)
    {
        NullabilityInfo nullabilityInfo = NullabilityContext.Create(property);
        return nullabilityInfo.ReadState == NullabilityState.Nullable;
    }
}
