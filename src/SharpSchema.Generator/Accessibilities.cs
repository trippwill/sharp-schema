namespace SharpSchema.Generator;

/// <summary>
/// Specifies the allowed accessibilities.
/// </summary>
[Flags]
public enum Accessibilities
{
    /// <summary>
    /// Indicates public accessibility.
    /// </summary>
    Public = 1,

    /// <summary>
    /// Indicates internal accessibility.
    /// </summary>

    Internal = 2,

    /// <summary>
    /// Indicates private accessibility.
    /// </summary>
    Private = 4,

    /// <summary>
    /// Indicates public or internal accessibility.
    /// </summary>
    PublicInternal = Public | Internal,

    /// <summary>
    /// Indicates any accessibility (public, internal, or private).
    /// </summary>
    Any = Public | Internal | Private,
}
