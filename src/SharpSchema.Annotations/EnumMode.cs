namespace SharpSchema.Annotations;

/// <summary>
/// Specifies how enums are handled in the generated code.
/// </summary>
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
enum EnumMode
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
