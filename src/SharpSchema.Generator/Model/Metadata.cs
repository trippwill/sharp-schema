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
public record MemberMeta(
    string Title,
    string? Description,
    List<string>? Examples,
    string? Comment,
    bool Deprecated)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var examplesString = Examples is not null ? string.Join(", ", Examples) : "None";
        return $"(Title: {Title}, Description: {Description}, Comment: {Comment}, Deprecated: {Deprecated}, Examples: [{examplesString}])";
    }

    /// <summary>
    /// A visitor that extracts member metadata from symbols.
    /// </summary>
    internal class SymbolVisitor : SymbolVisitor<MemberMeta>
    {
        private const string JsonSchemaTag = "jsonschema";
        private const string TitleElement = "title";
        private const string DescriptionElement = "description";
        private const string CommentElement = "comment";
        private const string ExampleElement = "example";
        private const string DeprecatedElement = "deprecated";

        public static SymbolVisitor Default { get; } = new();

        /// <summary>
        /// Visits a named type symbol and extracts <see cref="MemberMeta"/>.
        /// </summary>
        /// <param name="symbol">The named type symbol to visit.</param>
        /// <returns>The extracted <see cref="MemberMeta"/>.</returns>
        public override MemberMeta VisitNamedType(INamedTypeSymbol symbol) => CreateMetadata(symbol);

        /// <summary>
        /// Visits a property symbol and extracts <see cref="MemberMeta"/>.
        /// </summary>
        /// <param name="symbol">The property symbol to visit.</param>
        /// <returns>The extracted <see cref="MemberMeta"/>.</returns>
        public override MemberMeta VisitProperty(IPropertySymbol symbol) => CreateMetadata(symbol);

        /// <summary>
        /// Visits a parameter symbol and extracts <see cref="MemberMeta"/>.
        /// </summary>
        /// <param name="symbol">The parameter symbol to visit.</param>
        /// <returns>The extracted <see cref="MemberMeta"/>.</returns>
        public override MemberMeta VisitParameter(IParameterSymbol symbol) => CreateMetadata(symbol);

        /// <summary>
        /// Creates a <see cref="MemberMeta"/> instance from the given symbol.
        /// </summary>
        /// <param name="symbol">The symbol to extract metadata from.</param>
        /// <returns>The created <see cref="MemberMeta"/> instance.</returns>
        private static MemberMeta CreateMetadata(ISymbol symbol)
        {
            using var trace = Tracer.Enter(symbol.Name);

            string title = symbol.Name.Replace("_", " ").Humanize();
            string? description = null;
            List<string>? examples = null;
            string? comment = null;
            bool deprecated = false;

            // Try the symbol's own doc comment first
            string? xmlComment = symbol.GetDocumentationCommentXml();

            // If none found, and it's a parameter of a record, parse the record's doc for <param name="..."><jsonschema> data
            if (string.IsNullOrWhiteSpace(xmlComment) && symbol is IParameterSymbol parameterSymbol)
            {
                var containingType = parameterSymbol.ContainingSymbol?.ContainingType;
                if (containingType?.IsRecord == true)
                {
                    xmlComment = containingType.GetDocumentationCommentXml();
                    if (!string.IsNullOrEmpty(xmlComment))
                    {
                        var recordDoc = XDocument.Parse(xmlComment);
                        var paramElement = recordDoc.Descendants("param")
                            .FirstOrDefault(e => e.Attribute("name")?.Value == symbol.Name);

                        if (paramElement is not null && paramElement.Element(JsonSchemaTag) is XElement subNode)
                        {
                            title = subNode.Element(TitleElement)?.Value ?? title;
                            description = subNode.Element(DescriptionElement)?.Value;
                            comment = subNode.Element(CommentElement)?.Value;
                            examples = subNode.Elements(ExampleElement).Select(e => e.Value).ToList();
                            deprecated = subNode.Element(DeprecatedElement) is not null;
                        }
                    }
                }
            }

            // If comment was found, parse any top-level <jsonschema> (usually for properties, etc.)
            if (!string.IsNullOrEmpty(xmlComment))
            {
                var xmlDoc = XDocument.Parse(xmlComment);
                if (xmlDoc.Descendants(JsonSchemaTag).FirstOrDefault() is XElement element)
                {
                    title = element.Element(TitleElement)?.Value ?? title;
                    description = element.Element(DescriptionElement)?.Value ?? description;
                    comment = element.Element(CommentElement)?.Value ?? comment;
                    examples ??= [.. element.Elements(ExampleElement).Select(e => e.Value)];
                    if (element.Element(DeprecatedElement) is not null)
                        deprecated = true;
                }
            }

            // Merge any attribute-based metadata overrides
            if (symbol.GetAttributeData<SchemaMetaAttribute>() is AttributeData data)
            {
                title = data.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Title)) ?? title;
                description = data.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Description)) ?? description;
                comment = data.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Comment)) ?? comment;
                examples = data.GetNamedArgumentArray<string>(nameof(SchemaMetaAttribute.Examples)) ?? examples;
                deprecated = data.GetNamedArgument<bool>(nameof(SchemaMetaAttribute.Deprecated)) || deprecated;
            }

            return new MemberMeta(title, description, examples, comment, deprecated);
        }
    }
}
