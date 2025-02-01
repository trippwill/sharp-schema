using Json.Schema;

namespace SharpSchema.Generator.Model;

using Builder = JsonSchemaBuilder;

internal static class CommonSchemas
{
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
        .Maximum(byte.MaxValue)
        .Comment("System.Byte");

    public static Builder System_SByte => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(sbyte.MinValue)
        .Maximum(sbyte.MaxValue)
        .Comment("System.SByte");

    public static Builder System_Int16 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(short.MinValue)
        .Maximum(short.MaxValue)
        .Comment("System.Int16");

    public static Builder System_UInt16 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(ushort.MinValue)
        .Maximum(ushort.MaxValue)
        .Comment("System.UInt16");

    public static Builder System_Int32 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(int.MinValue)
        .Maximum(int.MaxValue)
        .Comment("System.Int32");

    public static Builder System_UInt32 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(uint.MinValue)
        .Maximum(uint.MaxValue)
        .Comment("System.UInt32");

    public static Builder System_Int64 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(long.MinValue)
        .Maximum(long.MaxValue)
        .Comment("System.Int64");

    public static Builder System_UInt64 => new Builder()
        .Type(SchemaValueType.Integer)
        .Minimum(ulong.MinValue)
        .Maximum(ulong.MaxValue)
        .Comment("System.UInt64");

    public static Builder System_Single => new Builder()
        .Type(SchemaValueType.Number)
        .Minimum(decimal.MinValue)
        .Maximum(decimal.MaxValue)
        .Comment("System.Single");

    public static Builder System_Double => new Builder()
        .Type(SchemaValueType.Number)
        .Minimum(decimal.MinValue)
        .Maximum(decimal.MaxValue)
        .Comment("System.Double");

    public static Builder System_Decimal => new Builder()
        .Type(SchemaValueType.Number)
        .Minimum(decimal.MinValue)
        .Maximum(decimal.MaxValue)
        .Comment("System.Decimal");

    public static Builder System_Char => new Builder()
        .Type(SchemaValueType.String)
        .MinLength(1)
        .MaxLength(1)
        .Comment("System.Char");

    public static Builder System_DateTime => new Builder()
        .Type(SchemaValueType.String)
        .Format(Formats.DateTime)
        .Comment("System.DateTime");

    public static Builder UnsupportedObject => new Builder()
        .Type(SchemaValueType.Object)
        .Title("Unsupported object");

    public static Builder DefRef(string key) => new Builder()
        .Ref(string.Format(DefUriFormat, key));

    public static Builder Nullable(Builder schema) => new Builder()
        .OneOf(schema, Null);

    public static Builder ArrayOf(Builder schema) => new Builder()
        .Type(SchemaValueType.Array)
        .Items(schema);
}
