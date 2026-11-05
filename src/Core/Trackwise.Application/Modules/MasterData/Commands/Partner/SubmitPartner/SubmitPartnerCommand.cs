using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Sends a draft partner to the approvers' queue. The steward's half of maker-checker - the half
/// that ends their authority over the record.
/// </summary>
public class SubmitPartnerCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }

    /// <summary>
    /// What the approver should know before deciding - where the details came from, which duplicate
    /// was ruled out. Optional: most partners speak for themselves.
    /// </summary>
    public string? Note { get; set; }
}
