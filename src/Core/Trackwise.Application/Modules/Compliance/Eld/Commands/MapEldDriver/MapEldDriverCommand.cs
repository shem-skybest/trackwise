using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Compliance.Eld.Commands;

[RequiresFeature(TenantFeature.Eld)]
public class MapEldDriverCommand : ICommand
{
    public Guid EmployeeId { get; set; }
    public EldProviderType ProviderType { get; set; }
    public required string ExternalDriverId { get; set; }
    public string? ExternalDriverName { get; set; }
}
