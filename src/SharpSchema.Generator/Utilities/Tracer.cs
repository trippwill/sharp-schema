using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SharpSchema.Generator.Utilities;

/// <summary>
/// Provides tracing functionality with indentation support.
/// To enable tracing output, set the <see cref="Writer"/> property to a delegate that writes a line of trace message.
/// </summary>
public static class Tracer
{
    /// <summary>
    /// Delegate for writing a line of trace message.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public delegate void LineWriter(string message);

    private static int s_indentLevel = 0;
    private static readonly string[] s_indents = new string[32];

    /// <summary>
    /// The number of spaces per indentation level.
    /// </summary>
    public static int IndentWidth = 2;

    /// <summary>
    /// The writer to use for writing trace messages.
    /// </summary>
    public static LineWriter? Writer;

    /// <summary>
    /// Writes a trace message followed by a newline.
    /// </summary>
    /// <param name="message">The message to write.</param>
    public static void WriteLine(string message) => Writer?.Invoke(message);

    /// <summary>
    /// Enters a new trace scope with the specified message.
    /// </summary>
    /// <param name="message">The message to write when entering the scope.</param>
    /// <param name="caller">The name of the caller member.</param>
    /// <returns>A <see cref="TraceScope"/> instance representing the new scope.</returns>
    public static TraceScope Enter(string message, [CallerMemberName] string? caller = null)
    {
        const string format = "{0}[{1}] {2}";
        WriteLine(string.Format(format, GetIndent(), caller, message));
        s_indentLevel++;
        return new TraceScope();
    }

    private static string GetIndent()
    {
        int indent = s_indentLevel;
        if (indent < s_indents.Length)
        {
            if (s_indents[indent] is null)
                s_indents[indent] = new string(' ', indent * IndentWidth);
            return s_indents[indent];
        }

        return string.Intern(new string(' ', indent * IndentWidth));
    }

    /// <summary>
    /// Represents a scope for tracing, which decreases the indent level when disposed.
    /// </summary>
    public readonly struct TraceScope() : IDisposable
    {
        /// <summary>
        /// Decreases the indent level when the scope is disposed.
        /// </summary>
        public void Dispose() => Tracer.s_indentLevel--;
    }
}
