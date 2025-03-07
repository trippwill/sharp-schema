using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace SharpSchema.Generator.TestData;

using SharpSchema.Annotations;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

using Test.Generator.RootSyntaxVisitorTests;

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
