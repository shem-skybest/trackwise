using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Types in the BKN-RFV number nSFS gave a Cold Chain booking, at any stage: nSFS often numbers it
/// only after the truck has loaded.
/// </summary>
public class SetBookingReferenceNumberCommand : ICommand<Result<string>>
{
    public Guid Id { get; set; }

    /// <summary>As nSFS or Monday shows it: "BKN-RFV1331-26-M", "BKN-RFV 1331" or "1331".</summary>
    public string ReferenceNumber { get; set; } = default!;
}
