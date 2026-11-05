using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.FuelLog.Commands;

/// <summary>Logs a fill-up for review.</summary>
public class RecordFuelFillUpCommand : FuelFillUpCommand, ICommand<Result<FuelFillUpDto>>;
