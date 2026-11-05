using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Abstractions.Modules.MasterData;

/// <summary>
/// How one master-data entity is governed: whether it needs approval, how duplicates are detected,
/// whether it is time-bound, and whether it is scoped to a branch.
/// <para>
/// This exists so the write pipeline never asks <c>if (entity is Customer)</c>. Behaviour that
/// differs per entity is data, declared once in <c>MasterDataPolicies</c>; adding an entity means
/// adding a row there, not a branch in a handler.
/// </para>
/// </summary>
public sealed record MasterDataPolicy
{
    /// <summary>CLR type name of the entity, e.g. <c>Branch</c>. The key stewards and code share.</summary>
    public required string EntityType { get; init; }

    public required MasterDataGroup Group { get; init; }

    /// <summary>
    /// Physical table the duplicate probe reads. Declared here rather than discovered from EF
    /// metadata because the probe drops to raw SQL for trigram matching. It is a compile-time
    /// constant from this configuration and never user input - see <c>DuplicateProbe</c>.
    /// </summary>
    public required string TableName { get; init; }

    /// <summary>
    /// Physical column backing <c>IMasterDataRecord.Code</c>, read by <c>DuplicateProbe</c>'s fixed
    /// select list. Defaults to <c>"code"</c>, the module's convention everywhere except
    /// <see cref="Domain.Entities.Truck"/>, whose column stays <c>number</c> - see
    /// <c>Truck.Number</c>'s doc comment for why it was never renamed.
    /// </summary>
    public string CodeColumn { get; init; } = "code";

    public MasterDataApprovalMode Approval { get; init; } = MasterDataApprovalMode.None;

    /// <summary>Rates are true only for a period; everything else is true until changed.</summary>
    public bool EffectiveDated { get; init; }

    /// <summary>Whether records carry a <c>BranchId</c> (null on the record = applies everywhere).</summary>
    public bool BranchScoped { get; init; }

    public required IReadOnlyList<DuplicateRule> DuplicateRules { get; init; }

    /// <summary>
    /// Where to look for records that would be left pointing at this one if a steward retires it.
    /// <para>
    /// Required rather than defaulted so that adding an entity forces an explicit answer. An empty
    /// list is a legitimate answer - it says "nothing references this yet" - but it has to be
    /// written down, because the failure mode of forgetting is a silent one: the probe simply finds
    /// nothing and every retirement looks safe.
    /// </para>
    /// </summary>
    public required IReadOnlyList<DependencyRule> DependencyRules { get; init; }

    /// <summary>
    /// History that a merge moves to the survivor on top of <see cref="DependencyRules" />. Leave a
    /// column out when it has a unique index that includes it: repointing would collide whenever
    /// both records have a row, and the failed UPDATE takes the whole merge down with it.
    /// </summary>
    public IReadOnlyList<MergeReference> MergeReferences { get; init; } = [];

    /// <summary>
    /// At or above this score the steward must either pick the candidate or explicitly declare the
    /// new record distinct. Creation is blocked until they do.
    /// </summary>
    public double HardThreshold { get; init; } = 0.90;

    /// <summary>At or above this score candidates are surfaced as a warning, but creation proceeds.</summary>
    public double SoftThreshold { get; init; } = 0.55;

    public bool RequiresApproval => Approval is MasterDataApprovalMode.MakerChecker;
}
