using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.FreightForwarding.Commands;

/// <summary>
/// Edits a workfile header. Null means "leave alone", matching every other update command here;
/// the handler merges against the stored value before handing the whole header to the entity.
/// </summary>
[RequiresFeature(TenantFeature.Workfiles)]
public class UpdateWorkfileHeaderCommand : ICommand<Result>
{
    public Guid Id { get; set; }
    public string? BillOfLadingNumber { get; set; }
    public BillIndicator? BillIndicator { get; set; }
    public string? VesselName { get; set; }
    public string? VoyageNumber { get; set; }
    public DateTime? Etd { get; set; }
    public DateTime? Eta { get; set; }
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public string? Commodity { get; set; }
    public Guid? ShippingLineId { get; set; }
    public Guid? CoLoaderId { get; set; }
    public Guid? ForwarderId { get; set; }
    public Guid? TruckerId { get; set; }
    public Guid? BrokerId { get; set; }
    public int? CreditTermDays { get; set; }
    public CreditType? CreditType { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? FinalExchangeRate { get; set; }
    public string? TaggingCategory { get; set; }
    public bool? IsAdvanceBilling { get; set; }
    public ReportCategory? ReportCategory { get; set; }
}
