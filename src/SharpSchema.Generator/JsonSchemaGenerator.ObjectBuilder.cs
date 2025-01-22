using Json.Schema;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;
using Node = SchemaNode;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public partial class JsonSchemaGenerator
{
    internal class ObjectBuilder(PropertyWalker propertyWalker)
    {
        public static readonly JsonSchema NullSchema = new Builder()
            .Type(SchemaValueType.Null);

        public Builder ApplyObject(Builder builder, Node.Object obj)
        {
            builder = obj switch
            {
                Node.Object.Custom d => ApplyObject(builder, d),
                Node.Object.Generic g => ApplyObject(builder, g),
                Node.Object.Nullable n => ApplyObject(builder, n),
                Node.Object.Override o => ApplyObject(builder, o),
                Node.Object.System s => ApplyObject(builder, s),
                _ => throw new NotSupportedException()
            };

            return builder.Apply(obj.Metadata);
        }

        protected virtual Builder ApplyObject(Builder builder, Node.Object.Custom obj)
        {
            return builder
                .Type(SchemaValueType.Object)
                .Properties([.. obj.Properties.Select(propertyWalker.Visit)]);
        }

        private Builder ApplyObject(Builder builder, Node.Object.Generic obj)
        {
            if (obj.TypeArguments.Length == 1)
            {
                // Array
                return builder
                    .Type(SchemaValueType.Array)
                    .Items(ApplyObject(new Builder(), obj.TypeArguments[0]));
            }

            if (obj.TypeArguments.Length == 2)
            {
                // Dictionary
                return builder
                    .Type(SchemaValueType.Object)
                    .PropertyNames(new Builder().Type(SchemaValueType.String))
                    .AdditionalProperties(ApplyObject(new Builder(), obj.TypeArguments[1]));
            }

            return builder
                .Type(SchemaValueType.Object)
                .Comment("Unsupported generic object");
        }

        private Builder ApplyObject(Builder builder, Node.Object.Nullable obj)
        {
            return builder.OneOf(
                ApplyObject(new Builder(), obj.ElementType),
                NullSchema);
        }

        private Builder ApplyObject(Builder builder, Node.Object.Override obj)
        {
            JsonSchema schema = JsonSchema.FromText(obj.SchemaString);
            return builder.Apply(schema);
        }

        private Builder ApplyObject(Builder builder, Node.Object.System obj)
        {
            return builder.Type(obj.TypeSymbol.GetSchemaValueType());
        }
    }

    internal class CachingObjectBuilder : ObjectBuilder
    {
        private readonly RootBuilder.Context _ctx;

        public CachingObjectBuilder(RootBuilder.Context context, PropertyWalker propertyWalker)
            : base(propertyWalker)
        {
            _ctx = context;
        }

        protected override Builder ApplyObject(Builder builder, Node.Object.Custom obj)
        {
            string cacheKey = obj.TypeSymbol.MetadataName;
            string refKey = $"#/$defs/{cacheKey}";
            if (_ctx.Defs.TryGetValue(cacheKey, out _))
                return new Builder().Ref(refKey);

            builder = base.ApplyObject(builder, obj);

            _ctx.Defs[cacheKey] = builder.Build();
            return new Builder().Ref(refKey);
        }
    }
}
