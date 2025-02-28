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
record GeneratorOptions(
    AccessibilityMode AccessibilityMode = AccessibilityMode.Public,
    TraversalMode TraversalMode = TraversalMode.Bases,
    DictionaryKeyMode DictionaryKeyMode = DictionaryKeyMode.Loose,
    EnumMode EnumMode = EnumMode.String,
    NumberMode NumberMode = NumberMode.StrictDefs)
{
    /// <summary>
    /// Gets the default generator options.
    /// </summary>
    public static GeneratorOptions Default { get; } = new GeneratorOptions();
}
