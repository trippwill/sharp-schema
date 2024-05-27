// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace SharpSchema.Tests;

public static class Examples
{
    public record SimplePerson(
        string Surname,
        string FamilyName,
        DateTime DateOfBirth,
        SimplePerson.RoleKind Role)
    {
        public enum RoleKind
        {
            User,
            SectionAdmin,
            SystemAdmin,
        }
    }
}
