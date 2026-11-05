using Trackwise.Application.Modules.IdentityAccess.Employees;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Operations;
using Trackwise.Application.Modules.Operations.Trips.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Mappings;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Operations.Trips.Queries;

internal sealed class GetTripHandler : IAppRequestHandler<GetTripQuery, Result<TripDto>>
{
    private readonly ITenantUnitOfWork _tenantUow;

    public GetTripHandler(ITenantUnitOfWork tenantUow)
    {
        _tenantUow = tenantUow;
    }

    public async Task<Result<TripDto>> Handle(
        GetTripQuery req, CancellationToken ct)
    {
        var trip = await _tenantUow.Repository<Trip>().GetByIdAsync(req.TripId);

        if (trip is null)
        {
            return Result<TripDto>.Fail($"Could not find a trip with ID '{req.TripId}'");
        }

        var truckNumber = await TruckRefResolver.ResolveNumberAsync(_tenantUow, trip.TruckId, ct);
        var names = new TripRefNames(
            truckNumber,
            await EmployeeRefResolver.ResolveNameAsync(_tenantUow, trip.DriverId, ct),
            await HelperRefResolver.ResolveNameAsync(_tenantUow, trip.HelperId, ct),
            await HelperRefResolver.ResolveNameAsync(_tenantUow, trip.SecondHelperId, ct));
        var dto = trip.ToDto(names) with
        {
            Rescues = await TripRescueReader.ForTripAsync(_tenantUow, trip.Id, ct)
        };
        return Result<TripDto>.Ok(dto);
    }
}
