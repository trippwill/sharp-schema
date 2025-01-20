namespace SharpSchema.Generator;

/// <summary>
/// Specifies the allowed type declarations.
/// </summary>
[Flags]
public enum AllowedTypeDeclarations
{
    /// <summary>
    /// Indicates a class type declaration.
    /// </summary>
    Class = 1,
    /// <summary>
    /// Indicates a struct type declaration.
    /// </summary>
    Struct = 2,
    /// <summary>
    /// Indicates a record type declaration.
    /// </summary>
    Record = 4,
    /// <summary>
    /// Indicates any type declaration (class, struct, or record).
    /// </summary>
    Any = Class | Struct | Record,
    /// <summary>
    /// Indicates the default type declaration (any).
    /// </summary>
    Default = Any,
}
