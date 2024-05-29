// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Json.Schema;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// A type handler that aggregates the individual type handlers.
/// </summary>
internal class AggregateTypeHandler : TypeHandler
{
    private static readonly TypeHandler[] TypeHandlers =
    [
        new NullableValueTypeHandler(),
        new OverrideValueTypeHandler(),
        new StringFormatTypeHandler(),
        new EnumAsStringTypeHandler(),
        new TypeCodeTypeHandler(),
        new DictionaryTypeHandler(),
        new EnumerableTypeHandler(),
        new FallbackTypeHandler(),
    ];

    /// <summary>
    /// Tries to handle the specified type and generate a JSON schema builder.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="context">The converter context.</param>
    /// <param name="type">The type to handle.</param>
    /// <param name="isRootType">Indicates whether the type is the root type.</param>
    /// <param name="propertyAttributeData">The custom attribute data for the properties.</param>
    /// <returns>A result indicating whether the type was handled successfully or not.</returns>
    public override Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        try
        {
            foreach (TypeHandler typeHandler in TypeHandlers)
            {
                Result result = typeHandler.TryHandle(builder, context, type, isRootType, propertyAttributeData);
                (builder, bool isHandled) = result.Unwrap();

                if (isHandled)
                {
                    return Result.Handled(builder);
                }
            }

            return Result.Fault(builder, $"No type handler found for type '{type.FullName}'.");
        }
        catch (Exception ex)
        {
            return Result.Fault(builder, ex.Message);
        }
    }
}
