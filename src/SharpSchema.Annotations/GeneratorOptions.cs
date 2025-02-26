using SharpSchema.Annotations;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace SharpSchema.Generator;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Represents the options for the generator.
/// </summary>
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
class GeneratorOptions
{
    /// <summary>
    /// Gets the default generator options.
    /// </summary>
    public static GeneratorOptions Default { get; } = new GeneratorOptions();

    /// <summary>
    /// Gets the accessibility mode.
    /// </summary>
    public AccessibilityMode AccessibilityMode { get; init; } = AccessibilityMode.Public;

    /// <summary>
    /// Gets the traversal mode.
    /// </summary>
    public TraversalMode TraversalMode { get; init; } = TraversalMode.SymbolOnly;

    /// <summary>
    /// Gets the dictionary key mode.
    /// </summary>
    public DictionaryKeyMode DictionaryKeyMode { get; init; } = DictionaryKeyMode.Loose;

    /// <summary>
    /// Gets the enum mode.
    /// </summary>
    public EnumMode EnumMode { get; init; } = EnumMode.String;

    /// <summary>
    /// Gets the number mode.
    /// </summary>
    public NumberMode NumberMode { get; init; } = NumberMode.StrictDefs;
}
