using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands.QuotationCommands.ApproveQuotation;

public class ApproveQuotationCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public string ApprovedBy { get; set; } = default!;
}
