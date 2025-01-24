namespace SharpSchema.Generator.Tests;

public class JsonSchemaGeneratorTests
{
    [Fact]
    public void Generate_ShouldThrowArgumentNullException_WhenRootInfoIsNull()
    {
        // Arrange
        JsonSchemaGenerator generator = new();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => generator.Generate(null!));
    }
}
