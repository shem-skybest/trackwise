using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Accounting.Queries;

/// <summary>
///     Get the current tenant's QuickBooks Online connection status for the settings page.
/// </summary>
[RequiresFeature(TenantFeature.Accounting)]
public class GetQuickBooksConnectionQuery : IQuery<Result<AccountingConnectionDto>>;
