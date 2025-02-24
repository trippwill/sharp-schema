using SharpSchema.Annotations;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace SharpSchema.Generator;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// The generator options.
/// </summary>
/// <param name="AccessibilityMode">The accessibility mode.</param>
/// <param name="TraversalMode">The traversal mode.</param>
/// <param name="DictionaryKeyMode">The dictionary key mode.</param>
/// <param name="EnumMode">The enum mode.</param>
#if SHARPSCHEMA_ASSEMBLY
public
#else
internal
#endif
record GeneratorOptions(
    AccessibilityMode AccessibilityMode = AccessibilityMode.Public,
    TraversalMode TraversalMode = TraversalMode.SymbolOnly,
    DictionaryKeyMode DictionaryKeyMode = DictionaryKeyMode.Loose,
    EnumMode EnumMode = EnumMode.String)
{
    /// <summary>
    /// Gets the default generator options.
    /// </summary>
    public static GeneratorOptions Default { get; } = new GeneratorOptions();
}
