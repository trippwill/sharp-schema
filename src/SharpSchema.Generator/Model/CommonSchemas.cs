using System.Text.Json.Nodes;
using Json.Schema;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator.Model;

using Builder = JsonSchemaBuilder;

internal static class CommonSchemas
{
    static CommonSchemas()
    {
        SchemaKeywordRegistry.Register<UnsupportedObjectKeyword>();
    }

    public const string DefUriFormat = "#/$defs/{0}";

    public static Builder Null => new Builder().Type(SchemaValueType.Null);

    public static Builder Boolean => new Builder().Type(SchemaValueType.Boolean);

    public static Builder Integer => new Builder().Type(SchemaValueType.Integer);

    public static Builder Number => new Builder().Type(SchemaValueType.Number);

    public static Builder String => new Builder().Type(SchemaValueType.String);

    public static Builder Array => new Builder().Type(SchemaValueType.Array);

    public static Builder Object => new Builder().Type(SchemaValueType.Object);

    public static Builder System_Byte => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(byte.MinValue)
        .Maximum(byte.MaxValue);

    public static Builder System_SByte => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(sbyte.MinValue)
        .Maximum(sbyte.MaxValue);

    public static Builder System_Int16 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(short.MinValue)
        .Maximum(short.MaxValue);

    public static Builder System_UInt16 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(ushort.MinValue)
        .Maximum(ushort.MaxValue);

    public static Builder System_Int32 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(int.MinValue)
        .Maximum(int.MaxValue);

    public static Builder System_UInt32 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(uint.MinValue)
        .Maximum(uint.MaxValue);

    public static Builder System_Int64 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(long.MinValue)
        .Maximum(long.MaxValue);

    public static Builder System_UInt64 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(ulong.MinValue)
        .Maximum(ulong.MaxValue);

    public static Builder System_Single => new Builder()
        .Type(SchemaValueType.Number)
        .Minimum(decimal.MinValue)
        .Maximum(decimal.MaxValue);

    public static Builder System_Double => new Builder()
        .Type(SchemaValueType.Number)
        .Minimum(decimal.MinValue)
        .Maximum(decimal.MaxValue);

    public static Builder System_Decimal => new Builder()
        .Type(SchemaValueType.Number)
        .Minimum(decimal.MinValue)
        .Maximum(decimal.MaxValue);

    public static Builder System_Char => new Builder()
        .Type(SchemaValueType.String)
        .MinLength(1)
        .MaxLength(1);

    public static Builder System_DateTime => new Builder()
        .Type(SchemaValueType.String)
        .Format(Formats.DateTime);

    public static Builder UnsupportedObject(string value) => new Builder()
        .UnsupportedObject(value);

    public static Builder UnsupportedObject(string format, params object[] args) => new Builder()
        .UnsupportedObject(string.Format(format, args));

    public static Builder DefRef(string key) => new Builder()
        .Ref(string.Format(DefUriFormat, key));

    public static Builder Nullable(Builder schema) => new Builder()
        .OneOf(schema, Null);

    public static Builder ArrayOf(Builder schema) => new Builder()
        .Type(SchemaValueType.Array)
        .Items(schema);

    public static Builder Const(JsonNode value) => new Builder()
        .Const(value);
}
