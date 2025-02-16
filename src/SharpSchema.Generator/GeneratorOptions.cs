namespace SharpSchema.Generator;

/// <summary>
/// The options for the generator.
/// </summary>
/// <param name="Accessibilities">The accessibilities to consider.</param>
/// <param name="Traversal">The traversal options.</param>
/// <param name="DictionaryKeyMode">The mode for dictionary keys.</param>
/// <param name="EnumHandling">The options for handling enums.</param>
public record GeneratorOptions(
    Accessibilities Accessibilities = Accessibilities.Public,
    Traversal Traversal = Traversal.SymbolOnly,
    DictionaryKeyMode DictionaryKeyMode = DictionaryKeyMode.Loose,
    EnumHandling EnumHandling = EnumHandling.String)
{
    /// <summary>
    /// Gets the default generator options.
    /// </summary>
    public static GeneratorOptions Default { get; } = new GeneratorOptions();
}
