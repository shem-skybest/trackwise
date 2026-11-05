using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Common;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.MasterData.Commands;

/// <summary>
/// Sends a pending partner back to its steward. The record returns to draft - a rejection is not a
/// verdict on the company, it is "this is not finished".
/// </summary>
public class RejectPartnerCommand : ICommand<Result>, IHaveId
{
    public Guid Id { get; set; }

    /// <summary>
    /// Why it is coming back. Required: a record returned without a reason is a record the steward
    /// resubmits unchanged, and the round trip has taught nobody anything.
    /// </summary>
    public string Reason { get; set; } = default!;
}
