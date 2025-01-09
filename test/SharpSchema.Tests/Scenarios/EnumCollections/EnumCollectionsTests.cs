using Json.Schema;
using SharpSchema;
using Xunit;
using Xunit.Abstractions;

namespace Scenarios.EnumCollections;

public class EnumCollectionsTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData(true, TrueExpectedSchema)]
    [InlineData(false, FalseExpectedSchema)]
    public void Convert_ExpectedSchema(bool enumAsUnderlyingType, string expectedSchema)
    {
        RootTypeContext context = RootTypeContext.FromType<UserData>();

        TypeConverter.Options options = new()
        {
            EnumAsUnderlyingType = enumAsUnderlyingType,
            ParseDocComments = true,
        };

        JsonSchema schema = new TypeConverter(options).Convert(context);
        string schemaString = schema.SerializeToJson();

        outputHelper.WriteLine(schemaString);

        Assert.Equal(expectedSchema, schemaString);
    }

    private const string TrueExpectedSchema = /*language=JSON*/ """
    {
      "$schema": "http://json-schema.org/draft-07/schema#",
      "type": "object",
      "properties": {
        "userName": {
          "type": "string",
          "title": "User Name"
        },
        "creationDate": {
          "$comment": "DateOnly",
          "type": "string",
          "format": "date",
          "title": "Creation Date"
        },
        "permissions": {
          "$comment": "PermissionKind[]",
          "type": "array",
          "items": {
            "type": "integer",
            "minimum": -2147483648,
            "maximum": 2147483647
          },
          "title": "The permissions granted to the user."
        },
        "areas": {
          "title": "The areas that the user can access.",
          "$comment": "AreaKind[]",
          "type": "array",
          "items": {
            "type": "integer",
            "minimum": -2147483648,
            "maximum": 2147483647
          },
          "minItems": 1,
          "maxItems": 3
        }
      },
      "required": [
        "userName",
        "creationDate",
        "permissions"
      ],
      "additionalProperties": false
    }
    """;

    private const string FalseExpectedSchema = /*language=JSON*/ """
    {
      "$schema": "http://json-schema.org/draft-07/schema#",
      "type": "object",
      "properties": {
        "userName": {
          "type": "string",
          "title": "User Name"
        },
        "creationDate": {
          "$comment": "DateOnly",
          "type": "string",
          "format": "date",
          "title": "Creation Date"
        },
        "permissions": {
          "$comment": "PermissionKind[]",
          "type": "array",
          "items": {
            "$ref": "#/$defs/Scenarios.EnumCollections.UserData_PermissionKind"
          },
          "title": "The permissions granted to the user."
        },
        "areas": {
          "title": "The areas that the user can access.",
          "$comment": "AreaKind[]",
          "type": "array",
          "items": {
            "$ref": "#/$defs/Scenarios.EnumCollections.UserData_AreaKind"
          },
          "minItems": 1,
          "maxItems": 3
        }
      },
      "required": [
        "userName",
        "creationDate",
        "permissions"
      ],
      "additionalProperties": false,
      "$defs": {
        "Scenarios.EnumCollections.UserData_AreaKind": {
          "$comment": "AreaKind",
          "type": "string",
          "enum": [
            "North Region",
            "South Region",
            "East Region",
            "West Region"
          ],
          "title": "Regions that a user can access."
        },
        "Scenarios.EnumCollections.UserData_PermissionKind": {
          "$comment": "PermissionKind",
          "type": "string",
          "enum": [
            "read",
            "write",
            "delete"
          ],
          "title": "Permissions that can be granted to a user."
        }
      }
    }
    """;
}
