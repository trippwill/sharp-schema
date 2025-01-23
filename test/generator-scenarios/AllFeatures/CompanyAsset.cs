using SharpSchema.Annotations;

namespace AllFeatures;

/// <summary>
/// A company asset.
/// </summary>
/// <param name="AssetId"> The unique identifier of the asset. </param>
/// <param name="AssetName"> The name of the asset. </param>
/// <jsonschema>
///     <description>Assets assigned to entities.</description>
/// </jsonschema>
[SchemaMeta(Description = "Assets of various types assigned to entities.")]
public abstract record CompanyAsset(string AssetId, string AssetName)
{
    public record Building(string AssetId, string AssetName, Address BuildingAddress) : CompanyAsset(AssetId, AssetName);

    public record Vehicle(string AssetId, string AssetName, string VehicleType) : CompanyAsset(AssetId, AssetName);

    public record Equipment(string AssetId, string AssetName, string EquipmentType) : CompanyAsset(AssetId, AssetName);
}
