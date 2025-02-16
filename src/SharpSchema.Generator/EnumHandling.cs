namespace SharpSchema.Generator;

/// <summary>
/// Specifies how enums are handled in the generated code.
/// </summary>
public enum EnumHandling
{
    /// <summary>
    /// Enum values are represented as strings.
    /// </summary>
    String,

    /// <summary>
    /// Enum values are represented as integers.
    /// </summary>
    UnderlyingType
}
