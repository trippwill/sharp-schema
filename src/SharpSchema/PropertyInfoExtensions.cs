// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json.Serialization;
using libanvl;
using SharpMeta;
using SharpSchema.Annotations;

namespace SharpSchema;

/// <summary>
/// Extension methods for <see cref="PropertyInfo"/>.
/// </summary>
internal static class PropertyInfoExtensions
{
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

        if (property.TryGetCustomAttributeData<SchemaIgnoreAttribute>(out _))
        {
            return true;
        }

        if (property.TryGetCustomAttributeData<JsonIgnoreAttribute>(out CustomAttributeData? ignoreAttributeData))
        {
            if (ignoreAttributeData.TryGetNamedArgument("Condition", out int condition))
            {
                return condition == (int)JsonIgnoreCondition.Always;
            }

            return true;
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
        string name = property.Name.ToJsonPropertyName();

        if (property.TryGetCustomAttributeData<JsonPropertyNameAttribute>(out CustomAttributeData? cad) &&
            cad.TryGetConstructorArgument(0, out string? jsonName))
        {
            name = jsonName;
        }

        return name;
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

        Opt<CustomAttributeData> schemaRequiredAttribute = property.GetCustomAttributeData<SchemaRequiredAttribute>();
        if (schemaRequiredAttribute)
        {
            Opt<bool> required = schemaRequiredAttribute.Select(sra => sra.GetConstructorArgument<bool>(0));
            return required.Unwrap();
        }

        if (property.IsRequiredMember())
        {
            return true;
        }

        Opt<CustomAttributeData> jsonRequiredAttribute = property.GetCustomAttributeData<JsonRequiredAttribute>();
        if (jsonRequiredAttribute)
        {
            return true;
        }

        Opt<CustomAttributeData> jsonIgnoreAttribute = property.GetCustomAttributeData<JsonIgnoreAttribute>();
        if (jsonIgnoreAttribute)
        {
            Opt<int> condition = jsonIgnoreAttribute.Select(jia => jia.GetNamedArgument<int?>("Condition"));
            return condition.Match(
                c => c switch
                {
                    (int)JsonIgnoreCondition.Always => false,
                    (int)JsonIgnoreCondition.WhenWritingDefault => false,
                    (int)JsonIgnoreCondition.WhenWritingNull => false,
                    _ => true,
                },
                () => false);
        }

        return !isNullable;
    }
}
