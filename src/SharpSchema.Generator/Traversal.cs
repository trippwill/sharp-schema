namespace SharpSchema.Generator;

/// <summary>
/// Specifies the traversal options for symbols.
/// </summary>
[Flags]
public enum Traversal
{
    /// <summary>
    /// No traversal options.
    /// </summary>
    None = 0,

    /// <summary>
    /// Traverse only the symbol itself.
    /// </summary>
    SymbolOnly = 1,

    /// <summary>
    /// Traverse the symbol and its base types.
    /// </summary>
    SymbolAndBases = 2,

    /// <summary>
    /// Traverse the symbol and its interfaces.
    /// </summary>
    SymbolAndInterfaces = 4,

    /// <summary>
    /// Traverse the symbol, its base types, and its interfaces.
    /// </summary>
    Full = SymbolAndBases | SymbolAndInterfaces,
}
