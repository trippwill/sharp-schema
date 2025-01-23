namespace SharpSchema.Generator.Utilities
{
    /// <summary>
    /// Provides extension methods for string operations.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Computes a hash code for the given string using a custom algorithm.
        /// </summary>
        /// <param name="value">The input string as a ReadOnlySpan of characters.</param>
        /// <returns>An long representing the hash code of the input string.</returns>
        public static long GetSchemaHash(this string? value)
        {
            if (value is null) return 0;

            ReadOnlySpan<char> str = value.AsSpan();
            unchecked
            {
                long hash = 23;
                foreach (char c in str)
                {
                    hash = (hash << 5) + c;
                }

                return hash;
            }
        }
    }
}
