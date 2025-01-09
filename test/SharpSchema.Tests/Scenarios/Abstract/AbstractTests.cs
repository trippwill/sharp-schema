// Copyright (c) Charles Willis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Json.More;
using Json.Schema;
using SharpSchema;
using Xunit;
using Xunit.Abstractions;

namespace Scenarios.Abstract;

public class AbstractTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void Convert_ExpectedSchema()
    {
        RootTypeContext typeContext = RootTypeContext.FromType<Person>() with
        {
            Id = "https://libanvl/test/scenario/abstract",
            CommonNamespace = "Scenarios.Abstract",
        };

        JsonSchema schema = new TypeConverter().Convert(typeContext);
        string schemaString = JsonSerializer.Serialize(
            schema.ToJsonDocument().RootElement,
            new JsonSerializerOptions { WriteIndented = true });

        outputHelper.WriteLine(schemaString);
        Assert.Equal(ExpectedSchema, schemaString);
    }

    private const string ExpectedSchema = /*lang=json*/ """
        {
          "$schema": "http://json-schema.org/draft-07/schema#",
          "$id": "https://libanvl/test/scenario/abstract",
          "title": "Person",
          "description": "A base record representing a person.",
          "oneOf": [
            {
              "$ref": "#/$defs/Person_Employee"
            },
            {
              "$ref": "#/$defs/Person_Customer"
            }
          ],
          "$defs": {
            "Person_Customer": {
              "title": "Customer",
              "description": "A record representing a customer.",
              "type": "object",
              "properties": {
                "customerId": {
                  "oneOf": [
                    {
                      "type": "string"
                    },
                    {
                      "type": "null"
                    }
                  ],
                  "title": "Customer Id"
                },
                "loyaltyLevel": {
                  "oneOf": [
                    {
                      "type": "string"
                    },
                    {
                      "type": "null"
                    }
                  ],
                  "title": "Loyalty Level"
                },
                "firstName": {
                  "type": "string",
                  "title": "First Name"
                },
                "lastName": {
                  "oneOf": [
                    {
                      "type": "string"
                    },
                    {
                      "type": "null"
                    }
                  ],
                  "title": "Last Name"
                },
                "age": {
                  "type": "integer",
                  "minimum": 0,
                  "maximum": 120,
                  "title": "Age"
                }
              },
              "required": [
                "customerId",
                "firstName",
                "lastName",
                "age"
              ],
              "additionalProperties": false
            },
            "Person_Employee": {
              "title": "Employee",
              "description": "A record representing an employee.",
              "type": "object",
              "properties": {
                "employeeId": {
                  "type": "string",
                  "title": "Employee Id"
                },
                "department": {
                  "type": "string",
                  "title": "Department"
                },
                "firstName": {
                  "type": "string",
                  "title": "First Name"
                },
                "lastName": {
                  "oneOf": [
                    {
                      "type": "string"
                    },
                    {
                      "type": "null"
                    }
                  ],
                  "title": "Last Name"
                },
                "age": {
                  "type": "integer",
                  "minimum": 0,
                  "maximum": 120,
                  "title": "Age"
                }
              },
              "required": [
                "employeeId",
                "firstName",
                "lastName",
                "age"
              ],
              "additionalProperties": false
            }
          }
        }
        """;
}
