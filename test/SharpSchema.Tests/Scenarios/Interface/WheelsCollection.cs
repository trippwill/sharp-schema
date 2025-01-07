// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using SharpSchema.Annotations;

namespace Scenarios.Interface;

[ExcludeFromCodeCoverage]
[SchemaRoot]
internal class WheelsCollection
{
    public required List<IHasWheels> Vehicles { get; init; }
}

[ExcludeFromCodeCoverage]
internal class Car : IHasWheels
{
    [SchemaRegex(@"[a-zA-Z0-9]*")]
    [SchemaLengthRange(Min = 1, Max = 20)]
    public required string Make { get; init; }

    public string? Model { get; init; }

    public int NumberOfWheels { get; init; } = 4;
}

[ExcludeFromCodeCoverage]
internal class Bicycle : IHasWheels
{
    public required string Manufacturer { get; init; }

    public int NumberOfWheels { get; init; } = 2;
}

internal interface IHasWheels
{
    [SchemaValueRange(Min = 0, Max = 18)]
    int NumberOfWheels { get; }
}
