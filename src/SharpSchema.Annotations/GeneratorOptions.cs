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
    /// Initializes a new instance of the <see cref="GeneratorOptions"/> class by copying the values from another instance.
    /// </summary>
    /// <param name="options">The <see cref="GeneratorOptions"/> instance to copy values from.</param>
    public GeneratorOptions(GeneratorOptions options)
    {
        if (options is null)
            throw new System.ArgumentNullException(nameof(options));

        AccessibilityMode = options.AccessibilityMode;
        TraversalMode = options.TraversalMode;
        DictionaryKeyMode = options.DictionaryKeyMode;
        EnumMode = options.EnumMode;
        NumberMode = options.NumberMode;
    }

    /// <summary>
    /// Gets the default generator options.
    /// </summary>
    public static GeneratorOptions Default { get; } = new GeneratorOptions();
}
