using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Integrations.Accounting.Queries;

/// <summary>
///     Build the QuickBooks Online OAuth2 authorization URL for the current tenant. The frontend
///     redirects the browser to the returned URL to begin the consent flow.
/// </summary>
[RequiresFeature(TenantFeature.Accounting)]
public class GetQuickBooksAuthUrlQuery : IQuery<Result<AccountingAuthUrlDto>>;
