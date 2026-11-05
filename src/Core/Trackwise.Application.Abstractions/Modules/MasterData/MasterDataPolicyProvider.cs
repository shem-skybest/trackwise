
namespace Trackwise.Application.Abstractions.Modules.MasterData;

public sealed class MasterDataPolicyProvider : IMasterDataPolicyProvider
{
    private static readonly Dictionary<string, MasterDataPolicy> ByEntityType =
        MasterDataPolicies.All.ToDictionary(p => p.EntityType, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MasterDataPolicy> All => MasterDataPolicies.All;

    public MasterDataPolicy? Find(string entityType) =>
        ByEntityType.GetValueOrDefault(entityType);
}
