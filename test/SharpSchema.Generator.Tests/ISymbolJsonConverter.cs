using Argon;
using Microsoft.CodeAnalysis;

namespace SharpSchema.Generator.Tests
{
    internal class ISymbolJsonConverter : Argon.JsonConverter<ISymbol>
    {
        public override ISymbol ReadJson(JsonReader reader, Type type, ISymbol? existingValue, bool hasExisting, Argon.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, ISymbol value, Argon.JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("SymbolKind");
            writer.WriteValue(value.Kind.ToString());
            writer.WritePropertyName("Name");
            writer.WriteValue(value.Name);
            writer.WriteEndObject();
        }
    }
}
