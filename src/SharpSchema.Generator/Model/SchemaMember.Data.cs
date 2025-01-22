using System.Xml.Linq;
using Humanizer;
using Microsoft.CodeAnalysis;
using SharpSchema.Annotations;

namespace SharpSchema.Generator.Model;

public abstract partial record SchemaMember
{
    /// <summary>
    /// Data for a schema member, including title, description, examples, comment, and deprecated status.
    /// </summary>
    /// <param name="Title">The title of the schema member.</param>
    /// <param name="Description">The description of the schema member.</param>
    /// <param name="Examples">Examples for the schema member.</param>
    /// <param name="Comment">Additional comments for the schema member.</param>
    /// <param name="Deprecated">Indicates if the schema member is deprecated.</param>
    public record Data(string Title, string? Description, List<string>? Examples, string? Comment, bool Deprecated)
    {
        /// <summary>
        /// A visitor that extracts member data from symbols.
        /// </summary>
        internal class SymbolVisitor : SymbolVisitor<Data>
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
            /// Visits a named type symbol and extracts <see cref="SchemaMember.Data"/>.
            /// </summary>
            /// <param name="symbol">The named type symbol to visit.</param>
            /// <returns>The extracted <see cref="Data"/>.</returns>
            public override Data VisitNamedType(INamedTypeSymbol symbol) => CreateData(symbol);

            /// <summary>
            /// Visits a property symbol and extracts <see cref="Data"/>.
            /// </summary>
            /// <param name="symbol">The property symbol to visit.</param>
            /// <returns>The extracted <see cref="Data"/>.</returns>
            public override Data VisitProperty(IPropertySymbol symbol) => CreateData(symbol);

            /// <summary>
            /// Creates a <see cref="Data"/> instance from the given symbol.
            /// </summary>
            /// <param name="symbol">The symbol to extract data from.</param>
            /// <returns>The created <see cref="Data"/> instance.</returns>
            private static Data CreateData(ISymbol symbol)
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

                return new Data(title, description, examples, comment, deprecated);
            }
        }
    }
}

