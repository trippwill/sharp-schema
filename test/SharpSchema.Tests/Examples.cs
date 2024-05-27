// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SharpSchema.Annotations;

namespace SharpSchema.Tests;

public static class Examples
{
    public record SimplePerson(
        string Surname,
        string? FamilyName,
        DateTime DateOfBirth,
        SimplePerson.RoleKind Role)
    {
        public enum RoleKind
        {
            User,
            SectionAdmin,
            SystemAdmin,
        }

        [SchemaIgnore]
        public int Age => (int)((DateTime.Now - this.DateOfBirth).TotalDays / 365.25);
    }

    public record SimpleOffice(
        SimplePerson[] Employees);
}
