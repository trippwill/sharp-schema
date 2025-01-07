using SharpSchema.Annotations;

namespace Scenarios.EnumCollections;

internal class UserData
{
    /// <summary>
    /// Permissions that can be granted to a user.
    /// </summary>
    public enum PermissionKind
    {
        Read,
        Write,
        Delete,
    }

    /// <summary>
    /// Regions that a user can access.
    /// </summary>
    public enum AreaKind
    {
        [SchemaEnumValue("North Region")]
        North,

        [SchemaEnumValue("South Region")]
        South,

        [SchemaEnumValue("East Region")]
        East,

        [SchemaEnumValue("West Region")]
        West,
    }

    public required string UserName { get; init; }

    public required DateOnly CreationDate { get; init; }

    /// <summary>
    /// The permissions granted to the user.
    /// </summary>
    public required DocFakeArray<PermissionKind> Permissions { get; init; }

    /// <summary>
    /// The areas that the user can access.
    /// </summary>
    [SchemaRequired(isRequired: false)]
    [SchemaItemsRange(Min = 1, Max = 3)]
    public required NoDocArray<AreaKind> Areas { get; init; }
}

/// <summary>
/// A fake array for testing resolution of doc comments.
/// </summary>
/// <remarks>
/// This is only used for testing how enumerable container doc comments are resolved.
/// </remarks>
/// <typeparam name="T"></typeparam>
internal class DocFakeArray<T> : List<T>
{
}

/// <devremarks>
/// When parsing doc comments, the summary for the property <see cref="UserData.Areas"/>
/// will be used instead of the 'Title' property for this type. A <see cref="SchemaMetaAttribute"/>
/// with the 'Title' property applied to <see cref="UserData.Areas"/> will also override
/// the 'Title' for this class.
/// </devremarks>
[SchemaMeta(Title = "Container for values")]
internal class NoDocArray<T> : List<T>
{
}
