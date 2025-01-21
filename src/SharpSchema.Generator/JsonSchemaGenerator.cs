using System.Runtime.InteropServices.ComTypes;
using Json.Schema;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;
using Member = SchemaMember;
using JsonSchemaResult = (JsonSchema Schema, string? Filename);
using PropertyResult = (string Name, JsonSchema Schema);
using DefMap = SortedDictionary<string, JsonSchema>;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public class JsonSchemaGenerator
{
    public JsonSchemaResult Generate(SchemaTree rootInfo)
    {
        return new RootBuilder(rootInfo).Build();
    }
}

public class RootBuilder
{
    public record struct Context(DefMap Defs, string? CommonNamespace);

    public static readonly JsonSchema NullSchema = new Builder()
        .Type(SchemaValueType.Null);

    private readonly SchemaTree _rootInfo;
    private readonly Context _ctx;
    private readonly PropertyWalker _propertyWalker;

    public RootBuilder(SchemaTree rootInfo)
    {
        Throw.IfNullArgument(rootInfo, nameof(rootInfo));

        _rootInfo = rootInfo;
        _ctx = new(
            new DefMap(StringComparer.Ordinal),
            rootInfo.CommonNamespace);

        _propertyWalker = new(_ctx);
    }

    public JsonSchemaResult Build()
    {
        Builder root = new();
        if (_rootInfo.Id is string id)
            root.Id(id);

        root.Apply(_rootInfo.RootType.MemberData);
        ApplyObject(root, _rootInfo.RootType);

        root.Defs(_ctx.Defs);

        return (root.Build(), _rootInfo.Filename);
    }

    private Builder ApplyObject(Builder builder, Member.Object obj)
    {
        return obj switch
        {
            Member.Object.DataObject d => ApplyObject(builder, d),
            Member.Object.GenericObject g => ApplyObject(builder, g),
            Member.Object.NullableObject n => ApplyObject(builder, n),
            Member.Object.OverrideObject o => ApplyObject(builder, o),
            Member.Object.SystemObject s => ApplyObject(builder, s),
            _ => throw new NotSupportedException()
        };
    }

    private Builder ApplyObject(Builder builder, Member.Object.DataObject obj)
    {
        return builder
            .Type(SchemaValueType.Object)
            .Properties([.. obj.Properties.Select(_propertyWalker.Visit)]);
    }

    private Builder ApplyObject(Builder builder, Member.Object.GenericObject obj)
    {
        if (obj.TypeArgumentMembers.Length == 1)
        {
            // Array
            return builder
                .Type(SchemaValueType.Array)
                .Items(ApplyObject(new Builder(), obj.TypeArgumentMembers[0]))
                .Apply(obj.MemberData);
        }

        if (obj.TypeArgumentMembers.Length == 2)
        {
            // Dictionary
            return builder
                .Type(SchemaValueType.Object)
                .PropertyNames(new Builder().Type(SchemaValueType.String))
                .AdditionalProperties(ApplyObject(new Builder(), obj.TypeArgumentMembers[1]))
                .Apply(obj.MemberData);
        }

        return builder
            .Type(SchemaValueType.Object)
            .Apply(obj.MemberData)
            .Comment("Unsupported generic object");
    }

    private Builder ApplyObject(Builder builder, Member.Object.NullableObject obj)
    {
        return builder.OneOf(
            ApplyObject(new Builder(), obj.ElementType),
            NullSchema);
    }

    private Builder ApplyObject(Builder builder, Member.Object.OverrideObject obj)
    {
        JsonSchema schema = JsonSchema.FromText(obj.Override!);
        return builder.Apply(schema);
    }

    private Builder ApplyObject(Builder builder, Member.Object.SystemObject obj)
    {
        return builder.Type(obj.TypeSymbol.GetSchemaValueType());
    }
}

public class PropertyWalker
{
    private readonly ObjectWalker _objectWalker;

    public PropertyWalker(RootBuilder.Context context)
    {
        _objectWalker = new(this, context);
    }

    public PropertyResult Visit(Member.Property property)
    {
        Builder builder = property switch
        {
            Member.Property.OverrideProperty o => Visit(o),
            Member.Property.DataProperty d => Visit(d),
            _ => throw new NotSupportedException()
        };

        return (
            property.PropertySymbol.Name,
            builder.Apply(property.MemberData));
    }

    private Builder Visit(Member.Property.OverrideProperty overrideProperty)
    {
        var schema = JsonSchema.FromText(overrideProperty.Override!);
        return new Builder()
            .Apply(schema);
    }

    private Builder Visit(Member.Property.DataProperty dataProperty)
    {
        return _objectWalker.Visit(dataProperty.MemberType);
    }
}

public class ObjectWalker
{
    private readonly PropertyWalker _propertyWalker;
    private readonly RootBuilder.Context _ctx;

    public ObjectWalker(PropertyWalker propertyWalker, RootBuilder.Context context)
    {
        _propertyWalker = propertyWalker;
        _ctx = context;
    }

    public Builder Visit(Member.Object obj)
    {
        Builder builder = obj switch
        {
            Member.Object.OverrideObject o => Visit(o),
            Member.Object.NullableObject n => Visit(n),
            Member.Object.SystemObject s => Visit(s),
            Member.Object.DataObject d => Visit(d),
            Member.Object.GenericObject g => Visit(g),
            _ => throw new NotSupportedException()
        };

        return builder.Apply(obj.MemberData);
    }

    private Builder Visit(Member.Object.DataObject obj)
    {
        string cacheKey = obj.TypeSymbol.Name;
        string refKey = $"#/$defs/{cacheKey}";
        if (_ctx.Defs.TryGetValue(cacheKey, out JsonSchema? schema))
            return new Builder().Ref(refKey);

        Builder builder = new();
        builder
            .Type(SchemaValueType.Object)
            .Properties([.. obj.Properties.Select(_propertyWalker.Visit)]);

        _ctx.Defs[cacheKey] = builder.Build();
        return new Builder().Ref(refKey);
    }

    private Builder Visit(Member.Object.GenericObject obj)
    {
        if (obj.TypeArgumentMembers.Length == 1)
        {
            // Array
            return new Builder()
                .Type(SchemaValueType.Array)
                .Items(Visit(obj.TypeArgumentMembers[0]))
                .Apply(obj.MemberData);
        }

        if (obj.TypeArgumentMembers.Length == 2)
        {
            // Dictionary
            return new Builder()
                .Type(SchemaValueType.Object)
                .PropertyNames(new Builder().Type(SchemaValueType.String))
                .AdditionalProperties(Visit(obj.TypeArgumentMembers[1]))
                .Apply(obj.MemberData);
        }

        return new Builder()
            .Type(SchemaValueType.Object)
            .Apply(obj.MemberData)
            .Comment("Unsupported generic object");
    }

    private Builder Visit(Member.Object.NullableObject obj)
    {
        return new Builder()
            .OneOf(
                Visit(obj.ElementType),
                RootBuilder.NullSchema);
    }

    private Builder Visit(Member.Object.OverrideObject obj)
    {
        var schema = JsonSchema.FromText(obj.Override!);
        return new Builder()
            .Apply(schema);
    }

    private Builder Visit(Member.Object.SystemObject obj)
    {
        return new Builder()
            .Type(obj.TypeSymbol.GetSchemaValueType());
    }
}
