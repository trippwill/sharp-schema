using System.Threading.Tasks;
using VerifyXunit;
using Xunit;

namespace SharpSchema.Test.Generator;

public class VerifyChecksTests
{
    [Fact]
    public Task Run() =>
        VerifyChecks.Run();
}
