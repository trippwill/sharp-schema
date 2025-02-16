namespace SharpSchema.Generator;

/// <summary>
/// Specifies the traversal options for symbols.
/// </summary>
[Flags]
public enum Traversal
{
    /// <summary>
    /// Traverse only the symbol itself.
    /// </summary>
    SymbolOnly = 0,

    /// <summary>
    /// Traverse the symbol and its base types.
    /// </summary>
    Bases = 2,

    /// <summary>
    /// Traverse the symbol and its interfaces.
    /// </summary>
    Interfaces = 4,

    /// <summary>
    /// Traverse the symbol, its base types, and its interfaces.
    /// </summary>
    Full = Bases | Interfaces
}
