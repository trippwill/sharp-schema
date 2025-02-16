using Json.More;
using Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json;
using SharpSchema.Generator.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace SharpSchema.Generator.Model;

/// <summary>
/// Handles `$unsupportedObject`.
/// </summary>
[SchemaKeyword(Name)]
[SchemaSpecVersion(SpecVersion.Draft6)]
[SchemaSpecVersion(SpecVersion.Draft7)]
[SchemaSpecVersion(SpecVersion.Draft201909)]
[SchemaSpecVersion(SpecVersion.Draft202012)]
[SchemaSpecVersion(SpecVersion.DraftNext)]
[Vocabulary(Vocabularies.Metadata201909Id)]
[Vocabulary(Vocabularies.Metadata202012Id)]
[Vocabulary(Vocabularies.MetadataNextId)]
[JsonConverter(typeof(UnsupportedObjectKeywordJsonConverter))]
[ExcludeFromCodeCoverage]
public class UnsupportedObjectKeyword : IJsonSchemaKeyword
{
    /// <summary>
    /// The JSON name of the keyword.
    /// </summary>
    public const string Name = "$unsupportedObject";

    /// <summary>
    /// The title.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new <see cref="UnsupportedObjectKeyword"/>.
    /// </summary>
    /// <param name="value">The title.</param>
    public UnsupportedObjectKeyword(string value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Builds a constraint object for a keyword.
    /// </summary>
    /// <param name="schemaConstraint">The <see cref="SchemaConstraint"/> for the schema object that houses this keyword.</param>
    /// <param name="localConstraints">
    ///     The set of other <see cref="KeywordConstraint"/>s that have been processed prior to this one.
    ///     Will contain the constraints for keyword dependencies.
    /// </param>
    /// <param name="context">The <see cref="EvaluationContext"/>.</param>
    /// <returns>A constraint object.</returns>
    public KeywordConstraint GetConstraint(SchemaConstraint schemaConstraint, ReadOnlySpan<KeywordConstraint> localConstraints, EvaluationContext context)
    {
        return KeywordConstraint.SimpleAnnotation(Name, Value);
    }
}

/// <summary>
/// JSON converter for <see cref="UnsupportedObjectKeyword"/>.
/// </summary>
public sealed class UnsupportedObjectKeywordJsonConverter : WeaklyTypedJsonConverter<UnsupportedObjectKeyword>
{
    /// <summary>Reads and converts the JSON to type <see cref="UnsupportedObjectKeyword"/>.</summary>
    /// <param name="reader">The reader.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    /// <returns>The converted value.</returns>
    public override UnsupportedObjectKeyword Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Expected string");

        var str = reader.GetString()!;

        return new UnsupportedObjectKeyword(str);
    }

    /// <summary>Writes a specified value as JSON.</summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The value to convert to JSON.</param>
    /// <param name="options">An object that specifies serialization options to use.</param>
    public override void Write(Utf8JsonWriter writer, UnsupportedObjectKeyword value, JsonSerializerOptions options)
    {
        Throw.IfNullArgument(writer);
        Throw.IfNullArgument(value);

        writer.WriteStringValue(value.Value);
    }
}
