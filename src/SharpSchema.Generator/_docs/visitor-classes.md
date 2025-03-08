# Visitor Classes in SharpSchema.Generator

## Class Responsibilities and Boundaries

This document outlines the responsibilities of `RootSyntaxVisitor.cs`, `LeafSyntaxVisitor.cs`, and `NamedTypeResolver.cs`, which form the core of SharpSchema's JSON schema generation process.

### 1. RootSyntaxVisitor

- Entry point for schema generation
- Manages the root schema document structure, setting up the schema version
- Handles top-level type declarations (classes, structs, records)
- Coordinates management of declarations for abstract types and their implementations
- Manages schema references via the `$defs` section
- Assembles the final complete schema with all required references

### 2. LeafSyntaxVisitor

- Handles the detailed syntax traversal of C# code
- Processes individual language elements like arrays, enums, nullable types, etc.
- Manages schema caching for defined types
- Creates references to abstract types for polymorphic schemas
- Translates various C# syntax nodes into JSON schema components
- Connects to NamedTypeResolver when processing types with members

### 3. NamedTypeResolver

- Focuses on symbol-based traversal rather than syntax
- Handles type member resolution (properties, record parameters)
- Manages property requirements based on nullability and attributes
- Implements base type and interface traversal based on TraversalMode
- Processes property metadata from attributes
- Manages property naming conventions and schema customization

## Class Collaboration Flow

1. `RootSyntaxVisitor` receives a type declaration and sets up the root schema
2. It calls through to `LeafSyntaxVisitor` to handle the details of the type
3. When a type with members is encountered, `LeafSyntaxVisitor` instantiates `NamedTypeResolver`
4. `NamedTypeResolver` processes the members, referring back to `LeafSyntaxVisitor` to resolve their types
5. `LeafSyntaxVisitor` caches completed schemas
6. `RootSyntaxVisitor` assembles everything into a complete schema with references

## Design Principles

- Single Responsibility: Each class has a focused area of concern
- Separation of Concerns: Syntax traversal vs. symbol traversal vs. high-level document structure
- Caching: Types are processed once and referenced thereafter
- Visitor Pattern: Standard Roslyn visitor pattern used throughout
