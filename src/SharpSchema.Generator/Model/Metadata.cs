using System.Collections;
using System.Xml.Linq;
using Humanizer;
using Microsoft.CodeAnalysis;
using SharpSchema.Annotations;
using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator.Model;

/// <summary>
/// Metadata for a schema member, including title, description, examples, comment, and deprecated status.
/// </summary>
/// <param name="Title">The title of the schema member.</param>
/// <param name="Description">The description of the schema member.</param>
/// <param name="Examples">Examples for the schema member.</param>
/// <param name="Comment">Additional comments for the schema member.</param>
/// <param name="Deprecated">Indicates if the schema member is deprecated.</param>
public record Metadata(string Title, string? Description, List<string>? Examples, string? Comment, bool Deprecated)
    : ISchemaNode
{
    /// <inheritdoc />
    public long GetSchemaHash() => SchemaHash.Combine(
        Title.GetSchemaHash(),
        Description.GetSchemaHash(),
        GetElementsSchemaHash(Examples),
        Comment.GetSchemaHash(),
        Deprecated ? 1 : 0);

    private static long GetElementsSchemaHash(List<string>? strings)
    {
        if (strings is null) return 0;

        long hash = 0;
        foreach (var str in strings)
        {
            hash = SchemaHash.Combine(hash, str.GetSchemaHash());
        }

        return hash;
    }

    /// <summary>
    /// A visitor that extracts member metadata from symbols.
    /// </summary>
    internal class SymbolVisitor : SymbolVisitor<Metadata>
    {
        private const string JsonSchemaTag = "jsonschema";
        private const string TitleElement = "title";
        private const string DescriptionElement = "description";
        private const string CommentElement = "comment";
        private const string ExampleElement = "example";
        private const string DeprecatedElement = "deprecated";

        /// <summary>
        /// Gets the singleton instance of the <see cref="SymbolVisitor"/> class.
        /// </summary>
        public static SymbolVisitor Instance { get; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolVisitor"/> class.
        /// </summary>
        private SymbolVisitor() { }

        /// <summary>
        /// Visits a named type symbol and extracts <see cref="Metadata"/>.
        /// </summary>
        /// <param name="symbol">The named type symbol to visit.</param>
        /// <returns>The extracted <see cref="Metadata"/>.</returns>
        public override Metadata VisitNamedType(INamedTypeSymbol symbol) => CreateMetadata(symbol);

        /// <summary>
        /// Visits a property symbol and extracts <see cref="Metadata"/>.
        /// </summary>
        /// <param name="symbol">The property symbol to visit.</param>
        /// <returns>The extracted <see cref="Metadata"/>.</returns>
        public override Metadata VisitProperty(IPropertySymbol symbol) => CreateMetadata(symbol);

        /// <summary>
        /// Visits a parameter symbol and extracts <see cref="Metadata"/>.
        /// </summary>
        /// <param name="symbol">The parameter symbol to visit.</param>
        /// <returns>The extracted <see cref="Metadata"/>.</returns>
        public override Metadata VisitParameter(IParameterSymbol symbol) => CreateMetadata(symbol);

        /// <summary>
        /// Creates a <see cref="Metadata"/> instance from the given symbol.
        /// </summary>
        /// <param name="symbol">The symbol to extract metadata from.</param>
        /// <returns>The created <see cref="Metadata"/> instance.</returns>
        private static Metadata CreateMetadata(ISymbol symbol)
        {
            string title = symbol.Name.Titleize();
            string? description = null;
            List<string>? examples = null;
            string? comment = null;
            bool deprecated = false;

            string? xmlComment = symbol.GetDocumentationCommentXml();
            if (!string.IsNullOrEmpty(xmlComment))
            {
                var xmlDoc = XDocument.Parse(xmlComment);
                if (xmlDoc.Descendants(JsonSchemaTag).FirstOrDefault() is XElement element)
                {
                    title = element.Element(TitleElement)?.Value ?? title;
                    description = element.Element(DescriptionElement)?.Value;
                    comment = element.Element(CommentElement)?.Value;
                    examples = [.. element.Elements(ExampleElement).Select(e => e.Value)];
                    deprecated = element.Element(DeprecatedElement) is not null;
                }
            }

            if (symbol.GetAttributeData<SchemaMetaAttribute>() is AttributeData data)
            {
                title = data.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Title)) ?? title;
                description = data.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Description)) ?? description;
                comment = data.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Comment)) ?? comment;
                examples = data.GetNamedArgument<List<string>>(nameof(SchemaMetaAttribute.Examples)) ?? examples;
                deprecated = data.GetNamedArgument<bool>(nameof(SchemaMetaAttribute.Deprecated));
            }

            return new Metadata(title, description, examples, comment, deprecated);
        }
    }
}
