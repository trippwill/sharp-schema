using System.Collections.Immutable;
using System.Text.RegularExpressions;
using SharpMeta;

namespace SharpSchema;

internal static partial class DocCommentsExtensions
{
    public static DocComments NormalizeForSchema(this DocComments source) =>
        new(
            source.Summary?.NormalizeForSchema(),
            source.Remarks?.NormalizeForSchema(),
            source.Returns?.NormalizeForSchema(),
            source.Parameters.NormalizeForSchema(),
            source.TypeParameters.NormalizeForSchema(),
            source.Exceptions.NormalizeForSchema(),
            [.. source.Examples.Select(NormalizeForSchema)]);

    [GeneratedRegex(@"<br\s*/?>", RegexOptions.IgnoreCase, "en-US")]
    internal static partial Regex BrTagRegex();

    [GeneratedRegex(@"</para\s*>", RegexOptions.IgnoreCase, "en-US")]
    internal static partial Regex EndParaTagRegex();

    [GeneratedRegex(@"<para\s*>", RegexOptions.IgnoreCase, "en-US")]
    internal static partial Regex StartParaTagRegex();

    [GeneratedRegex(@"<[^>]+>")]
    internal static partial Regex XmlTagRegex();

    [GeneratedRegex(@"\s*\n\s*")]
    internal static partial Regex WhitespaceNewLineRegex();

    [GeneratedRegex(@"\s+")]
    internal static partial Regex MultipleSpacesRegex();

    private static ImmutableArray<(string Name, string Value)> NormalizeForSchema(
        this ImmutableArray<(string Name, string Value)> lines)
    {
        if (lines.IsDefaultOrEmpty)
            return lines;

        ImmutableArray<(string Name, string Value)>.Builder builder = ImmutableArray.CreateBuilder<(string Name, string Value)>(lines.Length);

        foreach ((string Name, string Value) in lines)
        {
            builder.Add((Name, Value.NormalizeForSchema()));
        }

        return builder.ToImmutable();
    }

    private static string NormalizeForSchema(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        // Replace line endings with new lines
        input = input.Replace("\r\n", "\n").Replace("\r", "\n");

        // Replace <br/> and <br /> with new lines
        string result = BrTagRegex().Replace(input, "\n");

        // Replace <para> and </para> with double new lines
        result = StartParaTagRegex().Replace(result, string.Empty);
        result = EndParaTagRegex().Replace(result, "\n\n");

        // Remove other tags but preserve their content
        result = XmlTagRegex().Replace(result, string.Empty);

        // Normalize indentation
        result = WhitespaceNewLineRegex().Replace(result, "\n");

        // Collapse multiple spaces into a single space
        result = MultipleSpacesRegex().Replace(result, " ");

        return result.Trim();
    }
}
