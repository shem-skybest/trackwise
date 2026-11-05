using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Accepts a pending partner and takes it live. The checker's half.
/// <para>
/// Refused with <c>MASTER_DATA_SELF_APPROVAL</c> when the caller raised the submission. That is the
/// whole mechanism: a permission alone would let one person with the right role do both halves, so
/// the check is against who submitted <i>this</i> record, not against what the caller may do.
/// </para>
/// </summary>
public class ApprovePartnerCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }

    /// <summary>Optional note recorded with the decision.</summary>
    public string? Note { get; set; }
}
