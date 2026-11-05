using Trackwise.Application.Abstractions;
using Trackwise.Application.Modules.Common.Constants;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Trackwise.Application.Modules.IdentityAccess.Invitations.Queries;

internal sealed class ValidateInvitationTokenHandler(
    IMasterUnitOfWork masterUow,
    UserManager<User> userManager)
    : IAppRequestHandler<ValidateInvitationTokenQuery, Result<InvitationValidationResult>>
{
    public async Task<Result<InvitationValidationResult>> Handle(ValidateInvitationTokenQuery req, CancellationToken ct)
    {
        var invitation = await masterUow.Repository<Invitation>()
            .Query()
            .FirstOrDefaultAsync(i => i.Token == req.Token, ct);

        if (invitation is null)
        {
            return Result<InvitationValidationResult>.Ok(new InvitationValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid invitation link."
            });
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            var statusMessage = invitation.Status switch
            {
                InvitationStatus.Accepted => "This invitation has already been accepted.",
                InvitationStatus.Expired => "This invitation has expired.",
                InvitationStatus.Cancelled => "This invitation has been cancelled.",
                _ => "This invitation is no longer valid."
            };

            return Result<InvitationValidationResult>.Ok(new InvitationValidationResult
            {
                IsValid = false,
                ErrorMessage = statusMessage
            });
        }

        if (invitation.IsExpired)
        {
            return Result<InvitationValidationResult>.Ok(new InvitationValidationResult
            {
                IsValid = false,
                ErrorMessage = "This invitation has expired."
            });
        }

        // Check if user already exists
        var existingUser = await userManager.FindByEmailAsync(invitation.Email);
        var isAppUser = invitation.Type == InvitationType.AppUser;

        return Result<InvitationValidationResult>.Ok(new InvitationValidationResult
        {
            IsValid = true,
            Email = invitation.Email,
            TenantName = isAppUser
                ? PlatformConstants.PlatformName
                : invitation.Tenant?.CompanyName ?? invitation.Tenant?.Name ?? string.Empty,
            RoleDisplayName = isAppUser
                ? InvitationMapper.GetAppRoleDisplayName(invitation.AppRole)
                : InvitationMapper.GetRoleDisplayName(invitation.TenantRole),
            UserExists = existingUser is not null
        });
    }
}
