using System;

#nullable enable

namespace SharpSchema.Annotations;

/// <summary>
/// Specifies that the annotated element should be ignored during schema generation.
/// </summary>
[AttributeUsage(SupportedTypes | SupportedMembers)]
#if SHARPSCHEMA_ASSEMBLY
public class SchemaIgnoreAttribute : SchemaAttribute
#else
internal class SchemaIgnoreAttribute : SchemaAttribute
#endif
{
}
