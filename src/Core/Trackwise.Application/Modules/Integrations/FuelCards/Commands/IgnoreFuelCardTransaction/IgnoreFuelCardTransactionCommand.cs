using Trackwise.Application.Abstractions;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.FuelCards.Commands;

/// <summary>
/// Dismisses a pending fuel card transaction (e.g. cash advance or non-fleet purchase)
/// so it leaves the review queue without creating an expense.
/// </summary>
[RequiresFeature(TenantFeature.FuelCards)]
public class IgnoreFuelCardTransactionCommand : ICommand<Result>
{
    public Guid TransactionId { get; set; }
}
