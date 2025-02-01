using System;
using Argon;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SharpSchema.Generator.Tests.VerifyConverters;

internal class EqualsValueClauseSyntaxConverter : JsonConverter<EqualsValueClauseSyntax>
{
    public override EqualsValueClauseSyntax ReadJson(JsonReader reader, Type type, EqualsValueClauseSyntax? existingValue, bool hasExisting, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, EqualsValueClauseSyntax value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("Value");
        writer.WriteValue(value.Value.ToString());
        writer.WriteEndObject();
    }
}
