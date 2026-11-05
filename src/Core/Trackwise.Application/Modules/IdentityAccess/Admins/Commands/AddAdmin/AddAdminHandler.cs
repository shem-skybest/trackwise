using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Abstractions.Email;
using Trackwise.Application.Abstractions.Email.Models;
using Trackwise.Application.Modules.Common.Constants;
using Trackwise.Application.Utilities;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Options;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Identity.Roles;
using Trackwise.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Trackwise.Application.Modules.IdentityAccess.Admins.Commands;

internal sealed class AddAdminHandler(
    IMasterUnitOfWork masterUow,
    UserManager<User> userManager,
    IEmailSender emailSender,
    IEmailTemplateService emailTemplateService,
    ICurrentUserService currentUserService,
    IOptions<IdentityServerOptions> identityServerOptions)
    : IAppRequestHandler<AddAdminCommand, Result<AddAdminResult>>
{
    public async Task<Result<AddAdminResult>> Handle(AddAdminCommand req, CancellationToken ct)
    {
        var currentUserId = currentUserService.GetUserId();
        if (currentUserId is null)
        {
            return Result<AddAdminResult>.Fail("User not authenticated.");
        }

        // Existing user -> grant the Admin role immediately.
        var existingUser = await userManager.FindByEmailAsync(req.Email);
        if (existingUser is not null)
        {
            if (await userManager.IsInRoleAsync(existingUser, AppRoles.SuperAdmin))
            {
                return Result<AddAdminResult>.Fail("This user is already a super admin.");
            }

            if (await userManager.IsInRoleAsync(existingUser, AppRoles.Admin))
            {
                return Result<AddAdminResult>.Fail("This user is already an admin.");
            }

            var addResult = await userManager.AddToRoleAsync(existingUser, AppRoles.Admin);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return Result<AddAdminResult>.Fail($"Failed to grant admin role: {errors}");
            }

            return Result<AddAdminResult>.Ok(new AddAdminResult { Invited = false, UserId = existingUser.Id });
        }

        // No user yet -> create a pending app-user invitation and email it.
        var existingInvitation = await masterUow.Repository<Invitation>()
            .GetAsync(i => i.Email == req.Email
                        && i.Type == InvitationType.AppUser
                        && i.Status == InvitationStatus.Pending, ct);

        if (existingInvitation is not null)
        {
            return Result<AddAdminResult>.Fail("An active admin invitation already exists for this email.");
        }

        var currentUser = await userManager.FindByIdAsync(currentUserId.Value.ToString());

        var invitation = new Invitation
        {
            Email = req.Email,
            Token = TokenGenerator.GenerateSecureToken(64),
            TenantId = null,
            Type = InvitationType.AppUser,
            AppRole = AppRoles.Admin,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            InvitedByUserId = currentUserId.Value,
            PersonalMessage = req.PersonalMessage,
            LastSentAt = DateTime.UtcNow
        };

        // Sent before saving so a failed send leaves no pending invitation blocking the retry.
        if (!await SendAdminInvitationEmailAsync(invitation, currentUser?.GetFullName() ?? "An administrator"))
        {
            return Result<AddAdminResult>.Fail($"Could not send the invitation email to {req.Email}. Please try again.");
        }

        await masterUow.Repository<Invitation>().AddAsync(invitation, ct);
        await masterUow.SaveChangesAsync(ct);

        return Result<AddAdminResult>.Ok(new AddAdminResult { Invited = true });
    }

    private async Task<bool> SendAdminInvitationEmailAsync(Invitation invitation, string invitedByName)
    {
        var acceptUrl = $"{identityServerOptions.Value.UserFacingAuthority}/Account/AcceptInvitation?token={invitation.Token}";

        var model = new InvitationEmailModel
        {
            InvitedByName = invitedByName,
            CompanyName = PlatformConstants.PlatformName,
            TypeLabel = "administrator",
            RoleDisplayName = InvitationMapper.GetAppRoleDisplayName(invitation.AppRole),
            PersonalMessage = invitation.PersonalMessage,
            AcceptUrl = acceptUrl,
            ExpiresAt = invitation.ExpiresAt.ToString("MMMM dd, yyyy")
        };

        var body = await emailTemplateService.RenderAsync("Invitation", model);
        return await emailSender.SendEmailAsync(
            invitation.Email,
            $"You're invited to join {PlatformConstants.PlatformName} as an administrator",
            body);
    }
}
