using SharpSchema.Annotations;
using System.Text.Json.Serialization;

namespace AllFeatures;

/// <summary>
/// Root object for the schema representing a company.
/// </summary>
[SchemaRoot(
    CommonNamespace = "AllFeatures",
    Filename = "test.schema.json",
    Id = "https://sharpschema/test/company")]
public class Company
{
    /// <summary>
    /// The name of the company.
    /// </summary>
    /// <jsonschema>
    ///     <title>Company Name</title>
    ///     <description>The name of the company.</description>
    ///     <example>Acme Corp</example>
    /// </jsonschema>
    public required string Name { get; set; }

    /// <summary>
    /// The list of departments in the company.
    /// </summary>
    /// <jsonschema>
    ///     <title>Departments</title>
    ///     <description>A list of departments in the company.</description>
    /// </jsonschema>
    [SchemaRequired(false)]
    public required List<Department> Departments { get; set; }

    /// <summary>
    /// The dictionary of employees by their ID.
    /// </summary>
    /// <jsonschema>
    ///     <title>Employees</title>
    ///     <description>A dictionary of employees by their ID.</description>
    /// </jsonschema>
    public Dictionary<string, Employee>? Employees { get; set; }

    /// <summary>
    /// The address of the company.
    /// </summary>
    /// <jsonschema>
    ///     <title>Company Address</title>
    ///     <description>The address of the company.</description>
    /// </jsonschema>
    public Address CompanyAddress { get; set; }
}

/// <summary>
/// Represents a department in the company.
/// </summary>
public class Department
{
    /// <summary>
    /// The name of the department.
    /// </summary>
    /// <jsonschema>
    ///     <title>Department Name</title>
    ///     <description>The name of the department.</description>
    ///     <example>Human Resources</example>
    /// </jsonschema>
    public string? Name { get; set; }

    /// <summary>
    /// The list of teams in the department.
    /// </summary>
    /// <jsonschema>
    ///     <title>Teams</title>
    ///     <description>A list of teams in the department.</description>
    /// </jsonschema>
    public required List<Team> Teams { get; set; }
}

/// <summary>
/// Represents a team in a department.
/// </summary>
public class Team
{
    /// <summary>
    /// The name of the team.
    /// </summary>
    /// <jsonschema>
    ///     <title>Team Name</title>
    ///     <description>The name of the team.</description>
    ///     <example>Development Team</example>
    /// </jsonschema>
    public required string Name { get; set; }

    /// <summary>
    /// The list of projects the team is working on.
    /// </summary>
    /// <jsonschema>
    ///     <title>Projects</title>
    ///     <description>A list of projects the team is working on.</description>
    /// </jsonschema>
    [SchemaRequired(false)]
    public required List<Project> Projects { get; set; }
}

/// <summary>
/// Represents a project in a team.
/// </summary>
public class Project
{
    /// <summary>
    /// The name of the project.
    /// </summary>
    /// <jsonschema>
    ///     <title>Project Name</title>
    ///     <description>The name of the project.</description>
    ///     <example>Project X</example>
    /// </jsonschema>
    [SchemaRequired]
    public string? Name { get; set; }

    /// <summary>
    /// The budget of the project.
    /// </summary>
    /// <jsonschema>
    ///     <title>Project Budget</title>
    ///     <description>The budget of the project.</description>
    ///     <example>100000</example>
    /// </jsonschema>
    public decimal Budget { get; set; }

    /// <summary>
    /// The list of tasks in the project.
    /// </summary>
    /// <jsonschema>
    ///     <title>Tasks</title>
    ///     <description>A list of tasks in the project.</description>
    /// </jsonschema>
    public required List<Task> Tasks { get; set; }
}

/// <summary>
/// Represents a task in a project.
/// </summary>
public class Task
{
    /// <summary>
    /// The name of the task.
    /// </summary>
    /// <jsonschema>
    ///     <title>Task Name</title>
    ///     <description>The name of the task.</description>
    ///     <example>Design Database</example>
    /// </jsonschema>
    public string? Name { get; set; }

    /// <summary>
    /// The estimated hours to complete the task.
    /// </summary>
    /// <jsonschema>
    ///     <title>Estimated Hours</title>
    ///     <description>The estimated hours to complete the task.</description>
    ///     <example>40</example>
    /// </jsonschema>
    public int EstimatedHours { get; set; }
}

/// <summary>
/// Represents an employee in the company.
/// </summary>
public class Employee
{
    /// <summary>
    /// The name of the employee.
    /// </summary>
    /// <jsonschema>
    ///     <title>Employee Name</title>
    ///     <description>The name of the employee.</description>
    ///     <example>John Doe</example>
    /// </jsonschema>
    public required string Name { get; set; }

    /// <summary>
    /// The position of the employee.
    /// </summary>
    /// <jsonschema>
    ///     <title>Position</title>
    ///     <description>The position of the employee.</description>
    ///     <example>Software Engineer</example>
    /// </jsonschema>
    public string? Position { get; set; }

    /// <summary>
    /// The address of the employee.
    /// </summary>
    /// <jsonschema>
    ///     <title>Employee Address</title>
    ///     <description>The address of the employee.</description>
    /// </jsonschema>
    public Address EmployeeAddress { get; set; }
}

/// <summary>
/// Represents an address.
/// </summary>
public struct Address
{
    /// <summary>
    /// The street of the address.
    /// </summary>
    /// <jsonschema>
    ///     <title>Street</title>
    ///     <description>The street of the address.</description>
    ///     <example>123 Main St</example>
    /// </jsonschema>
    public string Street { get; set; }

    /// <summary>
    /// The city of the address.
    /// </summary>
    /// <jsonschema>
    ///     <title>City</title>
    ///     <description>The city of the address.</description>
    ///     <example>Metropolis</example>
    /// </jsonschema>
    public string City { get; set; }

    /// <summary>
    /// The postal code of the address.
    /// </summary>
    /// <jsonschema>
    ///     <title>Postal Code</title>
    ///     <description>The postal code of the address.</description>
    ///     <example>12345</example>
    /// </jsonschema>
    public string PostalCode { get; set; }

    /// <summary>
    /// The zip code of the address.
    /// </summary>
    /// <jsonschema>
    ///     <deprecated />
    /// </jsonschema>
    public string ZipCode { get; set; }
}
