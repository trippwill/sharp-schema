// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Json.Schema;
using Microsoft;

namespace SharpSchema.TypeHandlers;

/// <summary>
/// Handles types with common type codes.
/// </summary>
internal class TypeCodeTypeHandler : TypeHandler
{
    /// <inheritdoc />
    public override Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Boolean => Result.Handled(
                builder
                    .Comment("bool")
                    .Type(TypeCode.Boolean.ToSchemaValueType())),
            TypeCode.Byte => Result.Handled(
                builder
                    .Comment("byte")
                    .Type(TypeCode.Byte.ToSchemaValueType())
                    .Minimum(byte.MinValue)
                    .Maximum(byte.MaxValue)),
            TypeCode.Char => Result.Handled(
                builder
                    .Comment("char")
                    .Type(TypeCode.Char.ToSchemaValueType())
                    .Minimum(char.MinValue)
                    .Maximum(char.MaxValue)),
            TypeCode.DateTime => Result.Handled(
                builder
                    .Comment("DateTime")
                    .Type(TypeCode.DateTime.ToSchemaValueType())
                    .Format(Formats.DateTime)),
            TypeCode.Decimal => Result.Handled(
                builder
                    .Comment("decimal")
                    .Type(TypeCode.Decimal.ToSchemaValueType())
                    .Minimum(decimal.MinValue)
                    .Maximum(decimal.MaxValue)),
            TypeCode.Double => Result.Handled(
                builder
                    .Comment("double")
                    .Type(TypeCode.Double.ToSchemaValueType())
                    .Minimum(decimal.CreateSaturating(double.MinValue))
                    .Maximum(decimal.CreateSaturating(double.MaxValue))),
            TypeCode.Int16 => Result.Handled(
                builder
                    .Comment("short")
                    .Type(TypeCode.Int16.ToSchemaValueType())
                    .Minimum(short.MinValue)
                    .Maximum(short.MaxValue)),
            TypeCode.Int32 => Result.Handled(
                builder
                    .Comment("int")
                    .Type(TypeCode.Int32.ToSchemaValueType())
                    .Minimum(int.MinValue)
                    .Maximum(int.MaxValue)),
            TypeCode.Int64 => Result.Handled(
                builder
                    .Comment("long")
                    .Type(TypeCode.Int64.ToSchemaValueType())
                    .Minimum(long.MinValue)
                    .Maximum(long.MaxValue)),
            TypeCode.SByte => Result.Handled(
                builder
                    .Comment("sbyte")
                    .Type(TypeCode.SByte.ToSchemaValueType())
                    .Minimum(sbyte.MinValue)
                    .Maximum(sbyte.MaxValue)),
            TypeCode.Single => Result.Handled(
                builder
                    .Comment("float")
                    .Type(TypeCode.Single.ToSchemaValueType())
                    .Minimum(decimal.CreateSaturating(float.MinValue))
                    .Maximum(decimal.CreateSaturating(float.MaxValue))),
            TypeCode.String => Result.Handled(
                builder
                    .Comment("string")
                    .Type(TypeCode.String.ToSchemaValueType())),
            TypeCode.UInt16 => Result.Handled(
                builder
                    .Comment("ushort")
                    .Type(TypeCode.UInt16.ToSchemaValueType())
                    .Minimum(ushort.MinValue)
                    .Maximum(ushort.MaxValue)),
            TypeCode.UInt32 => Result.Handled(
                builder
                    .Comment("uint")
                    .Type(TypeCode.UInt32.ToSchemaValueType())
                    .Minimum(uint.MinValue)
                    .Maximum(uint.MaxValue)),
            TypeCode.UInt64 => Result.Handled(
                builder
                    .Comment("ulong")
                    .Type(TypeCode.UInt64.ToSchemaValueType())
                    .Minimum(ulong.MinValue)
                    .Maximum(ulong.MaxValue)),
            TypeCode.Object => Result.NotHandled(builder),
            _ => Assumes.NotReachable<Result>(),
        };
    }
}
