using Trackwise.Application.Abstractions.Common;
using Trackwise.Application.Attributes;
using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Application.Modules.Integrations.Accounting.Commands;

/// <summary>
///     Disconnect the current tenant's QuickBooks Online integration: revoke the token at the
///     provider and deactivate the stored configuration.
/// </summary>
[RequiresFeature(TenantFeature.Accounting)]
public class DisconnectQuickBooksCommand : ICommand;
