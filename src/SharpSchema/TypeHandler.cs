// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Humanizer;
using Json.Schema;
using Microsoft;

namespace SharpSchema;

/// <summary>
/// An abstract base class for type handling strategies.
/// </summary>
internal abstract class TypeHandler
{
    /// <summary>
    /// Tries to handle the specified type.
    /// </summary>
    /// <param name="builder">The JSON schema builder.</param>
    /// <param name="context">The global converter context.</param>
    /// <param name="type">The type to handle.</param>
    /// <param name="isRootType">Indicates whether the type is the root type.</param>
    /// <param name="propertyAttributeData">The custom attribute data for the owning property of the type.</param>
    /// <returns>A <see cref="Result"/> indicating whether the type was handled and any messages.</returns>
    public abstract Result TryHandle(JsonSchemaBuilder builder, ConverterContext context, Type type, bool isRootType = false, IList<CustomAttributeData>? propertyAttributeData = null);

    /// <summary>
    /// The result of a type handling operation.
    /// </summary>
    public record struct Result(bool IsHandled, JsonSchemaBuilder Builder, string[] Messages)
    {
        /// <summary>
        /// Creates a handled result with the specified builder and messages.
        /// </summary>
        /// <param name="builder">The JSON schema builder.</param>
        /// <param name="messages">The messages associated with the result.</param>
        /// <returns>The handled result.</returns>
        public static Result Handled(JsonSchemaBuilder builder, params string[] messages) => new(true, builder, messages);

        /// <summary>
        /// Creates a not handled result with the specified builder and messages.
        /// </summary>
        /// <param name="builder">The JSON schema builder.</param>
        /// <param name="messages">The messages associated with the result.</param>
        /// <returns>The not handled result.</returns>
        public static Result NotHandled(JsonSchemaBuilder builder, params string[] messages) => new(false, builder, messages);
    }
}
