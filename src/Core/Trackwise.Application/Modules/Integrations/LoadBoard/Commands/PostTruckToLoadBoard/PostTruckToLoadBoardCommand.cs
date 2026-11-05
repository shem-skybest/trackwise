using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.LoadBoard.Commands;

[RequiresFeature(TenantFeature.LoadBoard)]
public class PostTruckToLoadBoardCommand : ICommand<Result<PostTruckResultDto>>
{
    public Guid TruckId { get; set; }
    public LoadBoardProviderType ProviderType { get; set; }

    public required Address AvailableAtAddress { get; set; }
    public required GeoPoint AvailableAtLocation { get; set; }

    public Address? DestinationPreference { get; set; }
    public int? DestinationRadius { get; set; }

    public DateTime AvailableFrom { get; set; }
    public DateTime? AvailableTo { get; set; }

    public string? EquipmentType { get; set; }
    public int? MaxWeight { get; set; }
    public int? MaxLength { get; set; }
}
