using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SharpSchema.Generator.Utilities;

internal readonly struct TraceScope : IDisposable
{
    private const int IndentWidth = 2;
#if SCOPE
    private static readonly string[] s_indents = new string[32];
#else
    private static readonly string[] s_indents = Array.Empty<string>();
#endif

    private TraceScope(string message, [CallerMemberName] string? caller = null)
    {
        const string format = "{0}[{1}] {2}";
        Trace.WriteLine(string.Format(format, GetIndent(), caller, message));
        Trace.Indent();
    }

#if SCOPE
    public static TraceScope Enter(string message, [CallerMemberName] string? caller = null) => new(message, caller);
#else
    public static TraceScope Enter(string _, string? __ = null) => new();
#endif

#if SCOPE
    public void Dispose() => Trace.Unindent();
#else
    public void Dispose() {}
#endif

    private static string GetIndent()
    {
        int indent = Trace.IndentLevel;
        if (indent < s_indents.Length)
        {
            if (s_indents[indent] is null)
                s_indents[indent] = new string(' ', indent * IndentWidth);
            return s_indents[indent];
        }

        return string.Intern(new string(' ', indent * IndentWidth));
    }
}
