#pragma warning disable IDE0130 // Namespace does not match folder structure
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
namespace SharpSchema.Generator.TestData;

using System.Collections.Generic;
using SharpSchema.Annotations;

public class DictionaryKey_Default
{
    public Dictionary<string, string> StringKey { get; set; }

    public Dictionary<int, string> IntKey { get; set; }

    public Dictionary<Accessibility_Default, string> ClassKey { get; set; }
}

public class DictionaryKey_PropertyOverride
{
    public Dictionary<string, string> StringKey { get; set; }

    [SchemaDictionaryKeyMode(DictionaryKeyMode.Silent)]
    public Dictionary<int, string> IntKey { get; set; }

    [SchemaDictionaryKeyMode(DictionaryKeyMode.Loose)]
    public Dictionary<bool, string> BoolKey { get; set; }

    [SchemaDictionaryKeyMode(DictionaryKeyMode.Strict)]
    public Dictionary<Accessibility_Default, string> ClassKey { get; set; }
}

public class DictionaryKey_NestedOverride
{
    public DictionaryKey_Default Default { get; set; }

    public DictionaryKey_PropertyOverride PropertyOverride { get; set; }
}

public class Accessibility_Default
{
    public string Public { get; set; }

    internal string Internal { get; set; }

    protected string Protected { get; set; }

    protected internal string ProtectedInternal { get; set; }

    private string Private { get; set; }
}

[SchemaAccessibilityMode(AccessibilityMode.Internal | AccessibilityMode.Private)]
public class Accessibility_ClassOverride
{
    public string Public { get; set; }

    internal string Internal { get; set; }

    protected string Protected { get; set; }

    protected internal string ProtectedInternal { get; set; }

    private string Private { get; set; }
}

public class Accessibility_NestedDefault
{
    public Accessibility_Default Default { get; set; }

    public Accessibility_ClassOverride ClassOverride { get; set; }
}

[SchemaAccessibilityMode(AccessibilityMode.Any)]
public class Accessibility_NestedOverride
{
    private Accessibility_Default Default { get; set; }

    internal Accessibility_ClassOverride ClassOverride { get; set; }
}
