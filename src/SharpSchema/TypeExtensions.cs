// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Json.Schema;
using Microsoft;
using SharpSchema.Annotations;

namespace SharpSchema;

/// <summary>
/// Provides extension methods for <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Converts a <see cref="TypeCode"/> to a <see cref="SchemaValueType"/>.
    /// </summary>
    /// <param name="typeCode">The <see cref="TypeCode"/> to convert.</param>
    /// <returns>The corresponding <see cref="SchemaValueType"/>.</returns>
    public static SchemaValueType ToSchemaValueType(this TypeCode typeCode) => typeCode switch
    {
        TypeCode.Boolean => SchemaValueType.Boolean,
        TypeCode.Byte => SchemaValueType.Integer,
        TypeCode.Char => SchemaValueType.Integer,
        TypeCode.DateTime => SchemaValueType.String,
        TypeCode.Decimal => SchemaValueType.Number,
        TypeCode.Double => SchemaValueType.Number,
        TypeCode.Int16 => SchemaValueType.Integer,
        TypeCode.Int32 => SchemaValueType.Integer,
        TypeCode.Int64 => SchemaValueType.Integer,
        TypeCode.SByte => SchemaValueType.Integer,
        TypeCode.Single => SchemaValueType.Number,
        TypeCode.String => SchemaValueType.String,
        TypeCode.UInt16 => SchemaValueType.Integer,
        TypeCode.UInt32 => SchemaValueType.Integer,
        TypeCode.UInt64 => SchemaValueType.Integer,
        TypeCode.Object => SchemaValueType.Object,
        _ => Assumes.NotReachable<SchemaValueType>(),
    };

    /// <summary>
    /// Whether the specified <see cref="Type"/> is a numeric schema type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>Whether the type is numeric.</returns>
    public static bool IsSchemaNumeric(this Type type) => Type.GetTypeCode(type).ToSchemaValueType() is SchemaValueType.Integer or SchemaValueType.Number;

    /// <summary>
    /// Converts a <see cref="Type"/> to a definition name for JSON schema.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to convert.</param>
    /// <param name="context">The converter context.</param>
    /// <returns>The definition name for JSON schema.</returns>
    public static string ToDefinitionName(this Type type, ConverterContext context)
    {
        Requires.NotNull(type, nameof(type));
        Requires.NotNull(context, nameof(context));

        string? genericArgs = type.IsGenericType
            ? $"[{string.Join('_', type.GetGenericArguments().Select(t => t.ToDefinitionName(context)))}]"
            : string.Empty;

        string @namespace = type.DeclaringType?.Namespace ?? type.Namespace ?? string.Empty;
        if (context.DefaultNamespace is not null && @namespace.StartsWith(context.DefaultNamespace))
        {
            @namespace = @namespace[context.DefaultNamespace.Length..];
        }
        else if (@namespace.StartsWith("System.Collections.Immutable"))
        {
            @namespace = @namespace["System.Collections.Immutable".Length..];
        }
        else if (@namespace.StartsWith("System.Collections.Generic"))
        {
            @namespace = @namespace["System.Collections.Generic".Length..];
        }

        string typeName = type.Name.Split('`')[0];

        string finalName = type.DeclaringType is null
            ? typeName
            : $"{type.DeclaringType.Name}_{typeName}";

        return $"{@namespace}.{finalName}{genericArgs}".Trim().Trim('.');
    }

    /// <summary>
    /// Determines whether the specified <see cref="Type"/> implements any of the specified interface names.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="interfaceNames">The names of the interfaces to check for.</param>
    /// <returns><c><see langword="true"/></c> if the <see cref="Type"/> implements any of the specified interface names; otherwise, <see langword="false"/>.</returns>
    internal static bool ImplementsAnyInterface(this Type type, params (string? Namespace, string Name)[] interfaceNames)
    {
        if (interfaceNames.Contains((type.Namespace, type.Name)))
        {
            return true;
        }

        Type? test = type;
        while (test is not null)
        {
            foreach (Type iface in test.GetInterfaces())
            {
                if (interfaceNames.Contains((iface.Namespace, iface.Name)))
                {
                    return true;
                }
            }

            test = test.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Tries to unwrap a nullable type and get its underlying type.
    /// </summary>
    /// <param name="type">The type to unwrap.</param>
    /// <param name="underlyingType">When this method returns, contains the underlying type of the nullable type, if successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the type is a nullable type and the underlying type is successfully obtained; otherwise, <see langword="false"/>.</returns>
    internal static bool TryUnwrapNullable(this Type type, [NotNullWhen(true)] out Type? underlyingType)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition().Name == typeof(Nullable<>).Name)
        {
            underlyingType = type.GetGenericArguments()[0];
            return true;
        }

        underlyingType = null;
        return false;
    }

    /// <summary>
    /// Gets the subtypes of the specified <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The base <see cref="Type"/>.</param>
    /// <returns>An enumerable collection of subtypes.</returns>
    internal static IEnumerable<Type> GetSubTypes(this Type type)
    {
        Assembly assembly = type.Assembly;

        foreach (Type t in assembly.GetTypes())
        {
            if (t.IsSubclassOf(type))
            {
                if (t.TryGetCustomAttributeData(typeof(SchemaIgnoreAttribute), out _))
                {
                    continue;
                }

                if (t.IsAbstract)
                {
                    foreach (Type tt in t.GetSubTypes())
                    {
                        yield return tt;
                    }

                    continue;
                }

                yield return t;
            }
        }
    }

    /// <summary>
    /// Determines whether the specified <see cref="Type"/> is a probable dictionary.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to check.</param>
    /// <param name="keyType">When this method returns, contains the key type of the dictionary, if successful; otherwise, <see langword="null"/>.</param>
    /// <param name="valueType">When this method returns, contains the value type of the dictionary, if successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the type is a probable dictionary; otherwise, <see langword="false"/>.</returns>
    internal static bool IsProbableDictionary(this Type type, [NotNullWhen(true)] out Type? keyType, [NotNullWhen(true)] out Type? valueType)
    {
        keyType = null;
        valueType = null;

        if (!type.IsGenericType)
        {
            return false;
        }

        Type? inspectedType = type;

        while (inspectedType is not null)
        {
            if (IsProbableDictionaryImpl(inspectedType, out keyType, out valueType))
            {
                return true;
            }

            inspectedType = inspectedType.BaseType;
        }

        return false;

        static bool IsProbableDictionaryImpl(Type type, [NotNullWhen(true)] out Type? keyType, [NotNullWhen(true)] out Type? valueType)
        {
            keyType = null;
            valueType = null;

            Type[] interfaces = type.GetInterfaces();

            foreach (Type iface in interfaces)
            {
                if (iface.IsGenericType &&
                    iface.GetGenericTypeDefinition().Name == typeof(IEnumerable<>).Name &&
                    iface.GenericTypeArguments.SingleOrDefault() is Type pairType &&
                    pairType.IsGenericType &&
                    pairType.GetGenericTypeDefinition().Name == typeof(KeyValuePair<,>).Name)
                {
                    Type[] args = pairType.GetGenericArguments();
                    keyType = args[0];
                    valueType = args[1];

                    return true;
                }
            }

            return false;
        }
    }
}
