﻿namespace SharpSchema.Generator.Model;

public abstract partial record SchemaNode
{
    /// <summary>
    /// Represents a schema node interface.
    /// </summary>
    public interface ISchemaNode
    {
        /// <summary>
        /// Gets the hash value of the schema.
        /// </summary>
        /// <returns>A long value representing the schema hash.</returns>
        long GetSchemaHash();
    }
}
