using Json.Schema;
using SharpSchema.Generator.Model;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;
using Node = SchemaNode;
using PropertyResult = (string Name, JsonSchema Schema);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public partial class JsonSchemaGenerator
{
    internal class PropertyWalker
    {
        private readonly ObjectWalker _objectWalker;

        public PropertyWalker(RootBuilder.Context context)
        {
            _objectWalker = new(context, this);
        }

        public PropertyResult Visit(Node.Property property)
        {
            Builder builder = property switch
            {
                Node.Property.Override o => Visit(o),
                Node.Property.Custom d => Visit(d),
                _ => throw new NotSupportedException()
            };

            return (
                property.Symbol.Name,
                builder.Apply(property.Metadata));
        }

        private Builder Visit(Node.Property.Override overrideProperty)
        {
            var schema = JsonSchema.FromText(overrideProperty.SchemaString);
            return new Builder()
                .Apply(schema);
        }

        private Builder Visit(Node.Property.Custom dataProperty)
        {
            return _objectWalker.Visit(dataProperty.MemberType);
        }
    }
}
