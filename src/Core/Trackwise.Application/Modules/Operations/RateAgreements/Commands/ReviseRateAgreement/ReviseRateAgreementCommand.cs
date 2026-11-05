using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.RateAgreements.Commands;

/// <summary>
/// Starts a new price revision: a draft copy of an approved agreement from a later date. The current
/// one keeps pricing until the revision is approved.
/// </summary>
public class ReviseRateAgreementCommand : ICommand<Result<RateAgreementDto>>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public DateOnly EffectiveFrom { get; set; }
}
