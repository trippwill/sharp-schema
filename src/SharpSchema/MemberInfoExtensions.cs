// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;

namespace SharpSchema;

/// <summary>
/// Provides extension methods for <see cref="MemberInfo"/> instances.
/// </summary>
public static class MemberInfoExtensions
{
    /// <summary>
    /// Gets the custom attribute data for the specified <see cref="MemberInfo"/>.
    /// </summary>
    /// <param name="memberInfo">The <see cref="MemberInfo"/> to get the custom attribute data for.</param>
    /// <param name="includeInherited">Specifies whether to include custom attribute data from inherited members.</param>
    /// <returns>The custom attribute data for the specified <see cref="MemberInfo"/>.</returns>
    public static IList<CustomAttributeData>? GetAllCustomAttributeData(this MemberInfo memberInfo, bool includeInherited)
    {
        ArgumentNullException.ThrowIfNull(memberInfo);

        ImmutableList<CustomAttributeData>.Builder attributeData = ImmutableList.CreateBuilder<CustomAttributeData>();
        attributeData.AddRange(memberInfo.GetCustomAttributesData());

        if (!includeInherited)
        {
            return attributeData.ToImmutable();
        }

        Type? baseType;

        // If the MemberInfo is for a Type, iterate through the base types for the attribute.
        if (memberInfo is Type typeInfo)
        {
            baseType = typeInfo.BaseType;
            while (baseType is not null)
            {
                attributeData.AddRange(baseType.GetCustomAttributesData());
                baseType = baseType.BaseType;
            }

            return attributeData.ToImmutable();
        }

        // If the MemberInfo is for a PropertyInfo, or FieldInfo, iterate through the base types of the
        // declaring type, so see if the attribute is declared on a member of the same name.
        baseType = memberInfo switch
        {
            PropertyInfo pi => pi.DeclaringType,
            FieldInfo fi => fi.DeclaringType,
            _ => null,
        };

        while (baseType is not null)
        {
            foreach (Type type in baseType.GetInterfaces())
            {
                attributeData.AddRange(GetCustomAttributesDataFromTypeMember(type, memberInfo.Name));
            }

            attributeData.AddRange(GetCustomAttributesDataFromTypeMember(baseType, memberInfo.Name));

            baseType = baseType.BaseType;
        }

        return attributeData.ToImmutable();

        //// -- local functions --

        static List<CustomAttributeData> GetCustomAttributesDataFromTypeMember(Type type, string memberName)
        {
            List<CustomAttributeData> attributeData = [];

            MemberInfo[] membersInfos = type.GetMember(memberName);
            foreach (MemberInfo member in membersInfos)
            {
                attributeData.AddRange(member.GetCustomAttributesData());
            }

            return attributeData;
        }
    }
}
