using Json.Schema;
using Microsoft.CodeAnalysis;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Utilities;

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

        public virtual Builder ApplyObject(Builder builder, Node.Object obj)
        {
            builder = obj switch
            {
                Node.Object.Override o => ApplyObject(builder, o),
                Node.Object.System s => ApplyObject(builder, s),
                Node.Object.Nullable n => ApplyObject(builder, n),
                Node.Object.Map m => ApplyObject(builder, m),
                Node.Object.Array arr => ApplyObject(builder, arr),
                Node.Object.Abstract a => ApplyObject(builder, a),
                Node.Object.Generic g => ApplyObject(builder, g),
                Node.Object.Custom d => ApplyObject(builder, d),
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

        protected virtual Builder ApplyObject(Builder builder, Node.Object.Generic obj)
        {
            return ApplyObject(builder, obj.BaseType);
        }

        private Builder ApplyObject(Builder builder, Node.Object.Abstract obj)
        {
            return builder
                .Type(SchemaValueType.Object)
                .OneOf([.. obj.Implementations.Select(impl => ApplyObject(new Builder(), impl).Build())]);
        }

        private Builder ApplyObject(Builder builder, Node.Object.Array obj)
        {
            return builder
                .Type(SchemaValueType.Array)
                .Items(ApplyObject(new Builder(), obj.ElementType));
        }

        private Builder ApplyObject(Builder builder, Node.Object.Map obj)
        {
            return builder
                .Type(SchemaValueType.Object)
                .AdditionalProperties(ApplyObject(new Builder(), obj.ValueType));
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
            return builder.Type(obj.Symbol.GetSchemaValueType());
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
            string cacheKey = obj.Symbol.GetDocumentationCommentId() ?? obj.Symbol.MetadataName;
            string refKey = $"#/$defs/{cacheKey}";
            if (_ctx.Defs.TryGetValue(cacheKey, out _))
                return new Builder().Ref(refKey);

            builder = base.ApplyObject(builder, obj);

            _ctx.Defs[cacheKey] = builder.Build();
            return new Builder().Ref(refKey);
        }

        protected override Builder ApplyObject(Builder builder, Node.Object.Generic obj)
        {
            string cacheKey = obj.BaseType.Symbol.GetDocumentationCommentId() ?? obj.BaseType.Symbol.MetadataName;
            string refKey = $"#/$defs/{cacheKey}";
            if (_ctx.Defs.TryGetValue(cacheKey, out _))
                return new Builder().Ref(refKey);

            builder = base.ApplyObject(builder, obj.BaseType);

            _ctx.Defs[cacheKey] = builder.Build();
            return new Builder().Ref(refKey);
        }
    }
}
