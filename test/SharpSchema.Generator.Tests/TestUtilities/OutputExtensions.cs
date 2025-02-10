using Xunit.Abstractions;

namespace SharpSchema.Generator.Tests.TestUtilities;

internal static class OutputExtensions
{
    public static void WriteSeparator(this ITestOutputHelper output, int count = 60)
    {
        output.WriteLine(new string('\u2500', count));
    }
}
