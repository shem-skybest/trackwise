namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// Resolves the governing policy for a master-data entity.
/// </summary>
public interface IMasterDataPolicyProvider
{
    /// <summary>
    /// The policy for <paramref name="entityType"/> (a CLR type name such as <c>Branch</c>), or
    /// <c>null</c> when the entity is not governed by the master-data pipeline.
    /// </summary>
    MasterDataPolicy? Find(string entityType);

    /// <summary>Every configured policy, for the admin screens that list what is governed.</summary>
    IReadOnlyList<MasterDataPolicy> All { get; }
}
