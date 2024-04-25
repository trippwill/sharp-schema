// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Json.Schema;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles array types.
/// </summary>
internal class ArrayTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        if (!type.IsArray)
        {
            return Result.NotHandled(builder);
        }

        Type? elementType = type.GetElementType();
        if (elementType is null)
        {
            return Result.Fault(builder, "The array element type is null.");
        }

        try
        {
            builder = builder.AddArrayType(elementType, context);
            return Result.Handled(builder);
        }
        catch (Exception ex)
        {
            return Result.Fault(builder, ex.Message);
        }
    }
}
