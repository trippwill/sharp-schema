// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SharpSchema.Annotations;

namespace Scenarios.Abstract;

/// <summary>Person</summary>
/// <remarks>A base record representing a person.</remarks>
[SchemaMeta(Comment = "This is a test class.")]
internal abstract record Person
{
    public required string FirstName { get; init; }

    [SchemaRequired]
    public string? LastName { get; init; }

    [SchemaValueRange(Min = 0, Max = 120)]
    public int Age { get; init; }

    [SchemaIgnore]
    public string? InternalCode { get; init; }

    [SchemaMeta(Title = "Employee", Description = "A record representing an employee.")]
    internal record Employee : Person
    {
        public required string EmployeeId { get; init; }

        [SchemaRequired(isRequired: false)]
        public required string Department { get; init; }
    }

    [SchemaMeta(Title = "Customer", Description = "A record representing a customer.")]
    internal record Customer : Person
    {
        [SchemaRequired]
        public string? CustomerId { get; init; }

        public string? LoyaltyLevel { get; init; }
    }
}
