namespace Scenarios.Dictionaries;

/// <summary>
/// Represents the configuration settings with properties and sections.
/// </summary>
/// <remarks>
/// This class is used to store configuration settings in the form of dictionaries.
/// </remarks>
public class Configuration
{
    /// <summary>
    /// Gets or sets the properties of the configuration.
    /// </summary>
    /// <remarks>
    /// The properties dictionary contains key-value pairs where the key is the property name and the value is the property details.
    /// </remarks>
    public Dictionary<string, Property>? Properties { get; set; }

    /// <summary>
    /// Gets or sets the sections of the configuration.
    /// </summary>
    /// <remarks>
    /// The sections dictionary contains key-value pairs where the key is the section name and the value is the section details.
    /// </remarks>
    public Dictionary<string, Section>? Sections { get; set; }
}

/// <summary>
/// Represents a property in the configuration.
/// </summary>
/// <remarks>
/// This class is used to define the details of a property including its name, description, default value, and order.
/// </remarks>
public class Property
{
    /// <summary>
    /// Gets or sets a value indicating whether the property is required.
    /// </summary>
    public required bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the name of the property.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the property.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the default value of the property.
    /// </summary>
    public required string DefaultValue { get; set; }

    /// <summary>
    /// Gets or sets the order of the property.
    /// </summary>
    /// <remarks>
    /// The order is used to determine the position of the property in a list or display.
    /// </remarks>
    public int Order { get; set; } = 0;
}

/// <summary>
/// Represents a section in the configuration.
/// </summary>
/// <remarks>
/// This class is used to define the details of a section including its name, description, and order.
/// </remarks>
public class Section
{
    /// <summary>
    /// Gets or sets the name of the section.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the section.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Gets or sets the order of the section.
    /// </summary>
    /// <remarks>
    /// The order is used to determine the position of the section in a list or display.
    /// </remarks>
    public int Order { get; set; } = 0;
}
