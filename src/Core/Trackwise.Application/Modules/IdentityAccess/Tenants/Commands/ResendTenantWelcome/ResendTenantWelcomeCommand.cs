using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.IdentityAccess.Tenants.Commands;

public record ResendTenantWelcomeCommand(Guid TenantId) : ICommand<Result>;
