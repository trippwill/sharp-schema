using SharpSchema.Generator.Utilities;
using Xunit;

namespace SharpSchema.Generator.Tests
{
    public class StringExtensionsTests
    {
        [Fact]
        public void GetStableHash_NullInput_ReturnsZero()
        {
            // Arrange
            string? input = null;

            // Act
            long result = input.GetStableHash();

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void GetStableHash_EmptyString_ReturnsInitialHash()
        {
            // Arrange
            string input = string.Empty;

            // Act
            long result = input.GetStableHash();

            // Assert
            Assert.Equal(0x1505L, result);
        }

        [Fact]
        public void GetStableHash_TypicalString_ReturnsExpectedHash()
        {
            // Arrange
            string input = "test";

            // Act
            long result = input.GetStableHash();

            // Assert
            Assert.Equal(0x1A_E4A8_FDB6_CF13L, result);
        }

        [Fact]
        public void GetStableHash_SameString_ReturnsSameHash()
        {
            // Arrange
            string input1 = "consistent";
            string input2 = "consistent";

            // Act
            long result1 = input1.GetStableHash();
            long result2 = input2.GetStableHash();

            // Assert
            Assert.Equal(result1, result2);
        }

        [Fact]
        public void GetStableHash_DifferentStrings_ReturnDifferentHashes()
        {
            // Arrange
            string input1 = "string1";
            string input2 = "string2";

            // Act
            long result1 = input1.GetStableHash();
            long result2 = input2.GetStableHash();

            // Assert
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void GetStableHash_SameStringDifferentCase_ReturnDifferentHashes()
        {
            // Arrange
            string input1 = "case";
            string input2 = "CASE";
            // Act
            long result1 = input1.GetStableHash();
            long result2 = input2.GetStableHash();
            // Assert
            Assert.NotEqual(result1, result2);
        }

        [Fact]
        public void GetStableHash_VeryLongString_ReturnsExpectedHash()
        {
            // Arrange
            string input = new string('a', 2048);
            // Act
            long result = input.GetStableHash();
            // Assert
            Assert.Equal(0x6558_27FD_E959_1505L, result);
        }
    }
}
