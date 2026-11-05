using Trackwise.Application.Abstractions;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

public class CreateChargeCodeCommand : ICommand<Result<ChargeCodeDto>>
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public ChargeCategory Category { get; set; }
    public FreightMode[] ApplicableModes { get; set; } = [];
    public string DefaultUnit { get; set; } = default!;
    public bool RequiresApproval { get; set; } = false;
    public string? Description { get; set; }
    public string? TaxCode { get; set; }
}
