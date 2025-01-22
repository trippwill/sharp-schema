using Json.Schema;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;
using Node = SchemaNode;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public partial class JsonSchemaGenerator
{
    public class ObjectWalker
    {
        private readonly CachingObjectBuilder _objectBuilder;

        public ObjectWalker(RootBuilder.Context context, PropertyWalker propertyWalker)
        {
            _objectBuilder = new(context, propertyWalker);
        }

        public Builder Visit(Node.Object obj)
        {
            Throw.IfNullArgument(obj, nameof(obj));

            Builder builder = new();
            builder = _objectBuilder.ApplyObject(builder, obj)
                .Apply(obj.Metadata);

            return builder;
        }
    }
}
