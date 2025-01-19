using System.Xml.Linq;
using Humanizer;
using Microsoft.CodeAnalysis;
using SharpSchema.Annotations;

namespace SharpSchema.Generator.Model;

/// <summary>
/// Data for a schema member, including title, description, example, comment, and deprecated status.
/// </summary>
/// <param name="Title">The title of the schema member.</param>
/// <param name="Description">The description of the schema member.</param>
/// <param name="Example">An example for the schema member.</param>
/// <param name="Comment">Additional comments for the schema member.</param>
/// <param name="Deprecated">Indicates if the schema member is deprecated.</param>
public record SchemaMemberData(string Title, string? Description, string? Example, string? Comment, string? Deprecated)
{
    /// <summary>
    /// A visitor that extracts <see cref="SchemaMemberData"/> from symbols.
    /// </summary>
    internal class SymbolVisitor : SymbolVisitor<SchemaMemberData>
    {
        private const string JsonSchemaTag = "jsonschema";
        private const string TitleElement = "title";
        private const string DescriptionElement = "description";
        private const string CommentElement = "comment";
        private const string ExampleElement = "example";
        private const string DeprecatedElement = "deprecated";

        /// <summary>
        /// Visits a named type symbol and extracts <see cref="SchemaMemberData"/>.
        /// </summary>
        /// <param name="symbol">The named type symbol to visit.</param>
        /// <returns>The extracted <see cref="SchemaMemberData"/>.</returns>
        public override SchemaMemberData VisitNamedType(INamedTypeSymbol symbol)
        {
            return CreateData(symbol);
        }

        /// <summary>
        /// Visits a property symbol and extracts <see cref="SchemaMemberData"/>.
        /// </summary>
        /// <param name="symbol">The property symbol to visit.</param>
        /// <returns>The extracted <see cref="SchemaMemberData"/>.</returns>
        public override SchemaMemberData VisitProperty(IPropertySymbol symbol)
        {
            return CreateData(symbol);
        }

        /// <summary>
        /// Creates <see cref="SchemaMemberData"/> from a symbol.
        /// </summary>
        /// <param name="symbol">The symbol to extract data from.</param>
        /// <returns>The created <see cref="SchemaMemberData"/>.</returns>
        private static SchemaMemberData CreateData(ISymbol symbol)
        {
            string title = symbol.Name.Titleize();
            string? description = null;
            string? comment = null;
            string? example = null;
            string? deprecated = null;

            string? xmlComment = symbol.GetDocumentationCommentXml();
            if (!string.IsNullOrEmpty(xmlComment))
            {
                XDocument xmlDoc = XDocument.Parse(xmlComment);
                if (xmlDoc.Descendants(JsonSchemaTag).FirstOrDefault() is XElement element)
                {
                    title = element.Element(TitleElement)?.Value ?? title;
                    description = element.Element(DescriptionElement)?.Value;
                    comment = element.Element(CommentElement)?.Value;
                    example = element.Element(ExampleElement)?.Value;
                    deprecated = element.Element(DeprecatedElement)?.Value;
                }
            }

            if (symbol.GetAttributeData<SchemaMetaAttribute>() is AttributeData data)
            {
                title = data.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Title)) ?? title;
                description = data.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Description)) ?? description;
                comment = data.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Comment)) ?? comment;
                example = data.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Example)) ?? example;
                deprecated = data.GetNamedArgument<string>(nameof(SchemaMetaAttribute.Deprecated)) ?? deprecated;
            }

            return new SchemaMemberData(title, description, example, comment, deprecated);
        }
    }
}
