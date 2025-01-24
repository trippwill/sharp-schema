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

    /// <summary>
    /// Represents a simple office.
    /// </summary>
    /// <remarks>An office is a collection of employees.</remarks>
    /// <param name="Employees">The employees in the office.</param>
    [SchemaMeta(Title = "Simple Office", Examples = new[] { "Example 1", "Example 2" })]
    public record SimpleOffice(
        SimplePerson[] Employees);

    public class GenericType<T>
    {
        public required T Value { get; set; }
    }

    public abstract class AbstractBase
    {
        public required string BaseProperty { get; set; }
    }

    public class DerivedClass : AbstractBase
    {
        public required string DerivedProperty { get; set; }
    }

    [SchemaPropertiesRange(Min = 1, Max = 5)]
    public class CustomAttributeClass
    {
        [SchemaRequired(isRequired: false)]
        public required string Property1 { get; set; }

        public string? Property2 { get; set; }
    }
}
