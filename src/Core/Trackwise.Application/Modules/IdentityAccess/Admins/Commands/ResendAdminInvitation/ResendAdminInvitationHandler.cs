using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Email;
using Trackwise.Application.Abstractions.Email.Models;
using Trackwise.Application.Modules.Common.Constants;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Options;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Trackwise.Application.Modules.IdentityAccess.Admins.Commands;

internal sealed class ResendAdminInvitationHandler(
    IMasterUnitOfWork masterUow,
    IEmailSender emailSender,
    IEmailTemplateService emailTemplateService,
    IOptions<IdentityServerOptions> identityServerOptions)
    : IAppRequestHandler<ResendAdminInvitationCommand, Result>
{
    public async Task<Result> Handle(ResendAdminInvitationCommand req, CancellationToken ct)
    {
        var invitation = await masterUow.Repository<Invitation>()
            .Query()
            .FirstOrDefaultAsync(i => i.Id == req.Id && i.Type == InvitationType.AppUser, ct);

        if (invitation is null)
        {
            return Result.Fail("Invitation not found.");
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            return Result.Fail("Only pending invitations can be resent.");
        }

        if (invitation.IsExpired)
        {
            return Result.Fail("Cannot resend an expired invitation. Please create a new invitation.");
        }

        var acceptUrl = $"{identityServerOptions.Value.UserFacingAuthority}/Account/AcceptInvitation?token={invitation.Token}";

        var model = new InvitationEmailModel
        {
            InvitedByName = invitation.InvitedByUser?.GetFullName() ?? "An administrator",
            CompanyName = PlatformConstants.PlatformName,
            TypeLabel = "administrator",
            RoleDisplayName = InvitationMapper.GetAppRoleDisplayName(invitation.AppRole),
            AcceptUrl = acceptUrl,
            ExpiresAt = invitation.ExpiresAt.ToString("MMMM dd, yyyy")
        };

        var body = await emailTemplateService.RenderAsync("InvitationReminder", model);
        var sent = await emailSender.SendEmailAsync(
            invitation.Email,
            $"Reminder: You're invited to join {PlatformConstants.PlatformName}",
            body);

        if (!sent)
        {
            return Result.Fail($"Could not resend the invitation email to {invitation.Email}. Please try again.");
        }

        invitation.SendCount++;
        invitation.LastSentAt = DateTime.UtcNow;

        masterUow.Repository<Invitation>().Update(invitation);
        await masterUow.SaveChangesAsync(ct);

        return Result.Ok();
    }
}
