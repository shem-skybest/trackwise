using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.BillingBatches.Queries;

/// <summary>Per client billed by batch, the completed trips no batch has taken yet - what's left to bill.</summary>
public class GetUnbilledTripsQuery : IQuery<Result<IReadOnlyList<UnbilledCustomerDto>>>;
