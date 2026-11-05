using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trucks.Commands;

public class SetTruckGeolocationCommand : ICommand
{
    public SetTruckGeolocationCommand(TruckGeolocationDto geolocationData)
    {
        GeolocationData = geolocationData;
    }

    public TruckGeolocationDto GeolocationData { get; set; }
}
