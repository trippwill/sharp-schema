using SharpSchema.Generator.Utilities;

namespace SharpSchema.Generator.Model;

internal record ObjectAttributes(
    AttributeHandler AccessibilityMode,
    AttributeHandler EnumMode,
    AttributeHandler Meta,
    AttributeHandler Override,
    AttributeHandler PropertiesRange,
    AttributeHandler Root,
    AttributeHandler TraversalMode
);

internal record PropertyAttributes(
    AttributeHandler Const,
    AttributeHandler DictionaryKeyMode,
    AttributeHandler EnumMode,
    AttributeHandler Ignore,
    AttributeHandler ItemsRange,
    AttributeHandler Format,
    AttributeHandler LengthRange,
    AttributeHandler Meta,
    AttributeHandler Override,
    AttributeHandler Regex,
    AttributeHandler Required,
    AttributeHandler ValueRange
);
