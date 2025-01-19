using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using SharpSchema.Annotations;

namespace SharpSchema.Generator.Model;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

public abstract record SchemaMember(SchemaMemberData? Data, string? Override)
{
    public abstract record Object(INamedTypeSymbol TypeSymbol, SchemaMemberData? Data, string? Override)
        : SchemaMember(Data, Override)
    {
        public record OverrideObject(INamedTypeSymbol TypeSymbol, string Override)
            : Object(TypeSymbol, Data: null, Override);

        public record DataObject(
            INamedTypeSymbol TypeSymbol,
            SchemaMemberData Data,
            ImmutableArray<Property> Properties)
            : Object(TypeSymbol, Data, Override: null);

        internal class SymbolVisitor : SymbolVisitor<Object>
        {
            private readonly Property.SymbolVisitor _propertyVisitor;

            public SymbolVisitor(SchemaMemberData.SymbolVisitor dataVisitor)
            {
                _propertyVisitor = new(this);
                this.DataVisitor = dataVisitor;
            }

            internal SchemaMemberData.SymbolVisitor DataVisitor { get; }

            public override Object? VisitNamedType(INamedTypeSymbol symbol)
            {
                if (symbol.IsNamespace)
                    return null;

                if (symbol.GetAttributeData<SchemaOverrideAttribute>() is AttributeData schemaOverrideAttribute)
                {
                    string @override = schemaOverrideAttribute.GetConstructorArgument<string>(0)
                        ?? Throw.ForNullValue<string>("Override value is null");

                    return new OverrideObject(symbol, @override);
                }

                if (!symbol.IsValidForGeneration())
                    return null;

                if (symbol.IsIgnoredForGeneration())
                    return null;

                SchemaMemberData data = DataVisitor.VisitNamedType(symbol);

                ImmutableArray<Property> properties = symbol.GetMembers()
                    .OfType<IPropertySymbol>()
                    .SelectNotNull(p => p.Accept(_propertyVisitor))
                    .ToImmutableArray();

                return new DataObject(symbol, data, properties);
            }
        }
    }

    public abstract record Property(IPropertySymbol PropertySymbol, SchemaMemberData? Data, string? Override)
        : SchemaMember(Data, Override)
    {
        public record OverrideProperty(IPropertySymbol PropertySymbol, string Override)
            : Property(PropertySymbol, Data: null, Override);

        public record DataProperty(
            IPropertySymbol PropertySymbol,
            SchemaMemberData Data,
            SchemaMember.Object MemberType,
            bool IsRequired,
            bool IsDeprecated)
            : Property(PropertySymbol, Data, Override: null);

        internal class SymbolVisitor(Object.SymbolVisitor objectVisitor) : SymbolVisitor<Property>
        {
            private readonly Object.SymbolVisitor _objectVisitor = objectVisitor;

            public override Property? VisitProperty(IPropertySymbol symbol)
            {
                if (symbol.GetAttributeData<SchemaOverrideAttribute>() is AttributeData schemaOverrideAttribute)
                {
                    string @override = schemaOverrideAttribute.GetConstructorArgument<string>(0)
                        ?? Throw.ForNullValue<string>("Override value is null");

                    return new OverrideProperty(symbol, @override);
                }

                if (!symbol.IsValidForGeneration())
                    return null;

                if (symbol.IsIgnoredForGeneration())
                    return null;

                SchemaMemberData data = _objectVisitor.DataVisitor.VisitProperty(symbol);

                bool isRequired = symbol.IsRequired || symbol.GetAttributeData<JsonRequiredAttribute>() is not null;
                if (isRequired && symbol.GetAttributeData<SchemaRequiredAttribute>() is AttributeData schemaRequiredAttribute
                    && schemaRequiredAttribute.GetConstructorArgument<bool>(0) == false)
                {
                    isRequired = false;
                }

                bool isDeprecated = data.Deprecated is not null;

                return symbol.Type.Accept(_objectVisitor) is Object objectMember
                    ? new DataProperty(symbol, data, objectMember, isRequired, isDeprecated)
                    : null;
            }
        }
    }
}
