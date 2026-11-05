using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.FuelLog.Commands;

/// <summary>Corrects a fill-up that isn't verified yet. A flagged one goes back for review.</summary>
public class UpdateFuelFillUpCommand : FuelFillUpCommand, ICommand<Result<FuelFillUpDto>>
{
    public Guid Id { get; set; }
}
