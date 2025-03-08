namespace SharpSchema.Annotations;

/// <summary>
/// Specifies the mode for handling numeric types in the schema generation.
/// </summary>
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
enum NumberMode
{
    /// <summary>
    /// Uses the best matching type for numbers, adding range validation based on .NET type.
    /// Numeric subschema placed in $defs, and referenced inline.
    /// </summary>
    StrictDefs,

    /// <summary>
    /// Uses the best matching type for numbers, adding range validation based on .NET type.
    /// Numeric subschema are always placed inline.
    /// </summary>
    StrictInline,

    /// <summary>
    /// Uses the best matching json type for numbers, without adding range validation.
    /// </summary>
    JsonNative,
}
