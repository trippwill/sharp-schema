using System.Runtime.CompilerServices;
using System.Diagnostics;

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

    private static readonly string[] s_indents = new string[16];
    private static int s_indentLevel = 0;
    private static LineWriter? s_writer;
    private static int s_indentWidth = 2;
    private static bool s_enableTiming = false;

    /// <summary>
    /// Sets the writer to use for writing trace messages.
    /// </summary>
    public static LineWriter? Writer { set => s_writer = value; }

    /// <summary>
    /// Sets the number of spaces per indentation level.
    /// </summary>
    public static int IndentWidth { set => s_indentWidth = value; }

    /// <summary>
    /// Gets or sets whether timing is enabled.
    /// </summary>
    public static bool EnableTiming
    {
        get => s_enableTiming;
        set => s_enableTiming = value;
    }

    /// <summary>
    /// Writes a trace message followed by a newline.
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <param name="caller">The name of the caller member.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteLine(string message, [CallerMemberName] string? caller = null)
    {
        const string format = "{0}[{1}] {2}";
        s_writer?.Invoke(string.Format(format, GetIndent(), caller, message));
    }

    /// <summary>
    /// Enters a new trace scope with the specified message.
    /// </summary>
    /// <param name="message">The message to write when entering the scope.</param>
    /// <param name="caller">The name of the caller member.</param>
    /// <returns>A <see cref="TraceScope"/> instance representing the new scope.</returns>
    public static TraceScope Enter(string message, [CallerMemberName] string? caller = null)
    {
        WriteLine(message, caller);
        s_indentLevel++;
        return new TraceScope();
    }

    private static string GetIndent()
    {
        int indent = s_indentLevel;
        if (indent < s_indents.Length)
        {
            if (s_indents[indent] is null)
                s_indents[indent] = new string(' ', indent * s_indentWidth);
            return s_indents[indent];
        }

        return string.Intern(new string(' ', indent * s_indentWidth));
    }

    /// <summary>
    /// Represents a scope for tracing, which decreases the indent level when disposed.
    /// </summary>
    public readonly struct TraceScope() : IDisposable
    {
        private readonly Stopwatch? _stopwatch = Tracer.EnableTiming ? Stopwatch.StartNew() : null;

        /// <summary>
        /// Writes a trace message followed by a newline.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="caller">The name of the caller member.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLine(string message, [CallerMemberName] string? caller = null)
            => Tracer.WriteLine(message, caller);

        /// <summary>
        /// Decreases the indent level when the scope is disposed.
        /// </summary>
        public void Dispose()
        {
            if (Tracer.EnableTiming)
            {
                _stopwatch!.Stop();
                double seconds = _stopwatch.Elapsed.TotalSeconds;
                WriteLine($"Done [{seconds:0.00}s]", "Scope");
            }

            Tracer.s_indentLevel--;
        }
    }
}
