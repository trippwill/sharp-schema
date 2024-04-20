// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Humanizer;
using Json.More;
using Json.Schema;
using Microsoft;

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

        // skip properties with JsonIgnoreAttribute
        if (property.GetCustomAttributesData()
            .Any(a => a.AttributeType.FullName == "System.Text.Json.Serialization.JsonIgnoreAttribute"))
        {
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
        string name = property.Name;
        if (property.GetCustomAttributesData()
            .FirstOrDefault(cad => cad.AttributeType.FullName == "System.Text.Json.Serialization.JsonPropertyNameAttribute") is { ConstructorArguments: { Count: 1 } arguments })
        {
            name = (string)arguments[0].Value!;
        }

        return name.Camelize();
    }

    /// <summary>
    /// Sets the range for the specified property in the JSON schema.
    /// </summary>
    /// <param name="property">The property to set the range for.</param>
    /// <param name="propertySchema">The JSON schema builder for the property.</param>
    public static void SetRange(this PropertyInfo property, JsonSchemaBuilder propertySchema)
    {
        Type type = property.PropertyType;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            type = Nullable.GetUnderlyingType(type) ?? Assumes.NotReachable<Type>();
        }

        // if the property type is a number and has a range, set the minimum and maximum values
        if (type.IsNumber()
            && property.GetCustomAttributesData()
                .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.RangeAttribute") is { ConstructorArguments: { Count: 2 } rangeArguments })
        {
            if (rangeArguments[0].Value is int minInt)
            {
                propertySchema
                    .Minimum(minInt)
                    .Maximum((int)rangeArguments[1].Value!);
            }
            else if (rangeArguments[0].Value is double minDouble)
            {
                propertySchema
                    .Minimum(decimal.CreateSaturating(minDouble))
                    .Maximum(decimal.CreateSaturating((double)rangeArguments[1].Value!));
            }
            else
            {
                Assumes.NotReachable();
            }
        }

        // if the property is a string
        if (type == typeof(string))
        {
            // if the property has a regular expression attribute, set the pattern
            if (property.GetCustomAttributesData()
                .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.RegularExpressionAttribute") is { ConstructorArguments: { Count: 1 } regexArguments })
            {
                propertySchema.Pattern((string)regexArguments[0].Value!);
            }

            // if the property has a string length attribute, set the minimum and maximum lengths
            if (property.GetCustomAttributesData()
                .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.StringLengthAttribute") is { ConstructorArguments: { Count: 1 } lengthArguments })
            {
                if (lengthArguments[0].Value is uint length)
                {
                    propertySchema
                        .MinLength(length)
                        .MaxLength(length);
                }
                else
                {
                    Assumes.NotReachable();
                }
            }

            // if the property has a max length attribute, set the maximum length
            if (property.GetCustomAttributesData()
                .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.MaxLengthAttribute") is { ConstructorArguments: { Count: 1 } maxLengthArguments })
            {
                if (maxLengthArguments[0].Value is uint maxLength)
                {
                    propertySchema.MaxLength(maxLength);
                }
                else
                {
                    Assumes.NotReachable();
                }
            }

            // if the property has a min length attribute, set the minimum length
            if (property.GetCustomAttributesData()
                .FirstOrDefault(cad => cad.AttributeType.FullName == "System.ComponentModel.DataAnnotations.MinLengthAttribute") is { ConstructorArguments: { Count: 1 } minLengthArguments })
            {
                if (minLengthArguments[0].Value is uint minLength)
                {
                    propertySchema.MinLength(minLength);
                }
                else
                {
                    Assumes.NotReachable();
                }
            }
        }
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

        bool isForcedRequired = property.GetCustomAttributesData()
            .Any(a => a.AttributeType.FullName == "System.Text.Json.Serialization.JsonRequiredAttribute");

        return isForcedRequired || !isNullable;
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
            property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
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
