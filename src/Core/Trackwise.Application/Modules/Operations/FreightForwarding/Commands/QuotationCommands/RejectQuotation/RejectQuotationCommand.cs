using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.RejectQuotation;

public class RejectQuotationCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public string RejectedBy { get; set; } = default!;
    public string Reason { get; set; } = default!;
}
