using Json.Schema;

namespace SharpSchema.Generator;

/// <summary>
/// Specifies the mode for dictionary keys.
/// </summary>
public enum DictionaryKeyMode
{
    /// <summary>
    /// Loose mode allows any type of dictionary key,
    /// adding a $comment to the schema for non-string keys.
    /// </summary>
    Loose = 1,

    /// <summary>
    /// Strict mode requires dictionary keys to be strings,
    /// returning an $unsupportedObject for non-string keys.
    /// </summary>
    Strict,

    /// <summary>
    /// Silent mode allows any type of dictionary key.
    /// </summary>
    Silent,

    /// <summary>
    /// Skip mode skips properties with Dictionary of non-string keys.
    /// </summary>
    Skip,
}
