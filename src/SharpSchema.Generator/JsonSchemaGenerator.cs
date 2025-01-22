using Json.Schema;
using SharpSchema.Generator.Model;

namespace SharpSchema.Generator;

using Builder = JsonSchemaBuilder;
using JsonSchemaResult = (JsonSchema Schema, string? Filename);
using DefMap = SortedDictionary<string, JsonSchema>;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public partial class JsonSchemaGenerator
{
    public JsonSchemaResult Generate(SchemaTree rootInfo)
    {
        return new RootBuilder(rootInfo).Build();
    }

    public class RootBuilder
    {
        public record struct Context(DefMap Defs, string? CommonNamespace);

        private readonly SchemaTree _rootInfo;
        private readonly Context _ctx;
        private readonly PropertyWalker _propertyWalker;
        private readonly ObjectBuilder _objectBuilder;

        public RootBuilder(SchemaTree rootInfo)
        {
            Throw.IfNullArgument(rootInfo, nameof(rootInfo));

            _rootInfo = rootInfo;
            _ctx = new(
                new DefMap(StringComparer.Ordinal),
                rootInfo.CommonNamespace);

            _propertyWalker = new(_ctx);
            _objectBuilder = new(_propertyWalker);
        }

        public JsonSchemaResult Build()
        {
            Builder root = new();
            if (_rootInfo.Id is string id)
                root.Id(id);

            root.Apply(_rootInfo.RootType.Metadata);
            _objectBuilder.ApplyObject(root, _rootInfo.RootType);

            root.Defs(_ctx.Defs);

            return (root.Build(), _rootInfo.Filename);
        }
    }
}
