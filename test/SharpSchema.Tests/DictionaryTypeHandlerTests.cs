using Xunit;

namespace SharpSchema.Tests.TypeHandlers;

public class DictionaryTypeHandlerTests
{
    [Fact]
    public void TryHandle_NonStringKeyDictionary_ReturnsFault()
    {
        RootTypeContext rootTypeContext = RootTypeContext.FromType<Dictionary<int, string>>();

        InvalidOperationException expectedException = Assert.Throws<InvalidOperationException>(() =>
        {
            new TypeConverter().Convert(rootTypeContext);
        });

        Assert.Contains("Only dictionaries with string keys are supported.", expectedException.Message);
    }
}
