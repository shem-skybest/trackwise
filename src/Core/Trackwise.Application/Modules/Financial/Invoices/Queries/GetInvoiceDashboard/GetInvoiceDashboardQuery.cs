using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.Invoices.Queries;

/// <summary>
/// Gets invoice dashboard statistics including counts and amounts by status.
/// </summary>
public record GetInvoiceDashboardQuery : IQuery<Result<InvoiceDashboardDto>>;
