using System.Diagnostics;
using Xunit.Abstractions;

namespace SharpSchema.Generator.Tests.Utilities;

internal static class OutputExtensions
{
    public static TraceListener TraceListener(this ITestOutputHelper output) => new XUnitTraceListener(output);

    public static void WriteSeparator(this ITestOutputHelper output, int count = 60)
    {
        output.WriteLine(new string('\u2500', count));
    }

    internal class XUnitTraceListener(ITestOutputHelper output) : TraceListener
    {
        public override void Write(string? message) => output.WriteLine(message);

        public override void WriteLine(string? message) => output.WriteLine(message);
    }
}
