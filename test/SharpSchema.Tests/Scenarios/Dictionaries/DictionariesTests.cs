using Xunit.Abstractions;

namespace Scenarios.Dictionaries;

public class DictionariesTests(ITestOutputHelper outputHelper)
    : ScenarioTestBase<Configuration>(outputHelper)
{
    protected override string CommonNamespace => "Scenarios.Dictionaries";

    protected override string ExpectedSchema => /* lang=json */ """
    {
      "$schema": "http://json-schema.org/draft-07/schema#",
      "$id": "https://libanvl/test/scenario",
      "title": "Represents the configuration settings with properties and sections.",
      "description": "This class is used to store configuration settings in the form of dictionaries.",
      "type": "object",
      "properties": {
        "properties": {
          "oneOf": [
            {
              "$comment": "[String => Property]",
              "type": "object",
              "propertyNames": {
                "type": "string"
              },
              "additionalProperties": {
                "$ref": "#/$defs/Property"
              }
            },
            {
              "type": "null"
            }
          ],
          "title": "Gets or sets the properties of the configuration.",
          "description": "The properties dictionary contains key-value pairs where the key is the property name and the value is the property details."
        },
        "sections": {
          "oneOf": [
            {
              "$comment": "[String => Section]",
              "type": "object",
              "propertyNames": {
                "type": "string"
              },
              "additionalProperties": {
                "$ref": "#/$defs/Section"
              }
            },
            {
              "type": "null"
            }
          ],
          "title": "Gets or sets the sections of the configuration.",
          "description": "The sections dictionary contains key-value pairs where the key is the section name and the value is the section details."
        }
      },
      "additionalProperties": false,
      "$defs": {
        "Property": {
          "title": "Represents a property in the configuration.",
          "description": "This class is used to define the details of a property including its name, description, default value, and order.",
          "type": "object",
          "properties": {
            "isRequired": {
              "type": "boolean",
              "title": "Gets or sets a value indicating whether the property is required."
            },
            "name": {
              "type": "string",
              "title": "Gets or sets the name of the property."
            },
            "description": {
              "type": "string",
              "title": "Gets or sets the description of the property."
            },
            "defaultValue": {
              "type": "string",
              "title": "Gets or sets the default value of the property."
            },
            "order": {
              "type": "integer",
              "minimum": -2147483648,
              "maximum": 2147483647,
              "title": "Gets or sets the order of the property.",
              "description": "The order is used to determine the position of the property in a list or display."
            }
          },
          "required": [
            "isRequired",
            "name",
            "description",
            "defaultValue",
            "order"
          ],
          "additionalProperties": false
        },
        "Section": {
          "title": "Represents a section in the configuration.",
          "description": "This class is used to define the details of a section including its name, description, and order.",
          "type": "object",
          "properties": {
            "name": {
              "type": "string",
              "title": "Gets or sets the name of the section."
            },
            "description": {
              "type": "string",
              "title": "Gets or sets the description of the section."
            },
            "order": {
              "type": "integer",
              "minimum": -2147483648,
              "maximum": 2147483647,
              "title": "Gets or sets the order of the section.",
              "description": "The order is used to determine the position of the section in a list or display."
            }
          },
          "required": [
            "name",
            "description",
            "order"
          ],
          "additionalProperties": false
        }
      }
    }
    """;
}
