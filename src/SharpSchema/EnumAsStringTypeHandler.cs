// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Humanizer;
using Json.Schema;

namespace SharpSchema;

/// <summary>
/// Handles enum types by representing them as strings.
/// </summary>
internal class EnumAsStringTypeHandler : TypeHandler
{
    /// <inheritdoc/>
    public override Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        if (!type.IsEnum || context.EnumAsUnderlyingType)
        {
            return Result.NotHandled(builder);
        }

        string[] enumNames = Enum.GetNames(type);
        string[] kebabCaseEnumNames = new string[enumNames.Length];
        for (int i = 0; i < enumNames.Length; i++)
        {
            kebabCaseEnumNames[i] = enumNames[i].Kebaberize();
        }

        builder = builder
            .Comment(type.Name)
            .Type(SchemaValueType.String)
            .Enum(kebabCaseEnumNames);

        return Result.Handled(builder);
    }
}
