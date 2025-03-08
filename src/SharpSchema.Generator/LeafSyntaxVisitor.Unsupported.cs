namespace SharpSchema.Generator;

internal partial class LeafSyntaxVisitor
{
    internal static class Unsupported
    {
        public const string EnumMessage = "Failed to build schema for enum '{0}'.";
        public const string IdentifierMessage = "Failed to build schema for identifier '{0}'.";
        public const string DeclarationMessage = "Could not find declaration for identifier '{0}'.";
        public const string UnboundGenericMessage = "Failed to evaluate unbound generic type '{0}'.";
        public const string TypeSymbolMessage = "Node '{0}' does not produce a type symbol.";
        public const string DictionaryElementMessage = "Failed to build schema for dictionary element type '{0}'.";
        public const string KeyTypeMessage = "Key type '{0}' is not supported.";
        public const string ArrayElementMessage = "Failed to build schema for array element type '{0}'.";
        public const string GenericTypeMessage = "Failed to build schema for generic type '{0}'.";
        public const string NullableElementMessage = "Failed to build schema for nullable element type '{0}'.";
        public const string PredefinedTypeMessage = "Failed to build schema for predefined type '{0}'.";
        public const string TypeMessage = "Failed to build schema for type '{0}'.";
        public const string SymbolMessage = "Failed building schema for {0}";
    }
}
