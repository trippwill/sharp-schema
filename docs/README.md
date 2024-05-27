# SharpSchema

SharpSchema is a very opiniated tool for transforming C# class hierarchies into JSON schema.

It is designed to work with your `System.Text.Json` deserialization library. `SchemaSharp.Annotations` provides
attributes you can apply to your DTOs to express JSON-Schema validation constraints that aren't possible
to express with pure C#.
