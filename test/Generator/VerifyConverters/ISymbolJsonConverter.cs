using System;
using Argon;
using Microsoft.CodeAnalysis;

namespace SharpSchema.Test.Generator.VerifyConverters
{
    internal class ISymbolJsonConverter : JsonConverter<ISymbol>
    {
        public override ISymbol ReadJson(JsonReader reader, Type type, ISymbol? existingValue, bool hasExisting, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, ISymbol value, JsonSerializer serializer)
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
