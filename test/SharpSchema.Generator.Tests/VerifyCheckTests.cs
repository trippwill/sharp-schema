using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace SharpSchema.Generator.Tests;

public class VerifyChecksTests
{
    [Fact]
    public Task Run() =>
        VerifyChecks.Run();
}
