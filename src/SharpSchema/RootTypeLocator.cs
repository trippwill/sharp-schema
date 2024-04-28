// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft;
using SharpSchema.Annotations;

namespace SharpSchema;

/// <summary>
/// Loads root types from an assembly.
/// </summary>
public static class RootTypeLocator
{
    private static readonly string[] SystemNamespaces = new[]
    {
        "System",
        "Microsoft.Windows",
        "Windows",
        "SharpSchema",
    };

    /// <summary>
    /// Retrieves the root type contexts from the specified assembly.
    /// </summary>
    /// <param name="assembly">The assembly to retrieve the root type contexts from.</param>
    /// <returns>An enumerable collection of <see cref="RootTypeContext"/> objects.</returns>
    public static IEnumerable<RootTypeContext> GetRootTypeContexts(Assembly assembly)
    {
        Requires.NotNull(assembly, nameof(assembly));

        foreach (TypeInfo type in assembly.GetTypes())
        {
            try
            {
                if (type.Namespace is not null && SystemNamespaces.Any(sns => type.Namespace.StartsWith(sns, StringComparison.Ordinal)))
                {
                    continue;
                }

                if (type.IsPrimitive)
                {
                    continue;
                }

                if (type.IsSpecialName)
                {
                    continue;
                }
            }
            catch (TypeLoadException)
            {
                // Ignore types that cannot be loaded
                continue;
            }
            catch (StackOverflowException)
            {
                // Ignore types that cause a stack overflow
                continue;
            }

            IList<CustomAttributeData> customAttributeData = type.GetCustomAttributesData();
            if (customAttributeData.Count == 0)
            {
                continue;
            }

            foreach (CustomAttributeData cad in customAttributeData)
            {
                if (cad.AttributeType.Name == typeof(SchemaRootAttribute).Name)
                {
                    yield return new RootTypeContext(
                        type,
                        Filename: cad.GetNamedArgument<string>("Filename"),
                        Id: cad.GetNamedArgument<string>("Id"),
                        CommonNamespace: cad.GetNamedArgument<string>("CommonNamespace"));
                }
            }
        }
    }
}
