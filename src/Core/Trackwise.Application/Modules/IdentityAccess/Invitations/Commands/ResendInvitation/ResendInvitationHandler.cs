using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Email.Models;
using Trackwise.Application.Abstractions.Email;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Options;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Mappings;
using Trackwise.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Trackwise.Application.Modules.IdentityAccess.Invitations.Commands;

internal sealed class ResendInvitationHandler(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow,
    IEmailSender emailSender,
    IEmailTemplateService emailTemplateService,
    IOptions<IdentityServerOptions> identityServerOptions)
    : IAppRequestHandler<ResendInvitationCommand, Result>
{
    public async Task<Result> Handle(ResendInvitationCommand req, CancellationToken ct)
    {
        var tenant = tenantUow.GetCurrentTenant();

        var invitation = await masterUow.Repository<Invitation>()
            .Query()
            .FirstOrDefaultAsync(i => i.Id == req.Id && i.TenantId == tenant.Id, ct);

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

        if (!await SendInvitationEmailAsync(invitation, tenant))
        {
            return Result.Fail($"Could not resend the invitation email to {invitation.Email}. Please try again.");
        }

        invitation.SendCount++;
        invitation.LastSentAt = DateTime.UtcNow;

        masterUow.Repository<Invitation>().Update(invitation);
        await masterUow.SaveChangesAsync(ct);

        return Result.Ok();
    }

    private async Task<bool> SendInvitationEmailAsync(Invitation invitation, Tenant tenant)
    {
        var acceptUrl = $"{identityServerOptions.Value.UserFacingAuthority}/Account/AcceptInvitation?token={invitation.Token}";
        var companyName = tenant.CompanyName ?? tenant.Name;

        var model = new InvitationEmailModel
        {
            InvitedByName = invitation.InvitedByUser?.GetFullName() ?? "A team member",
            CompanyName = companyName,
            TypeLabel = invitation.Type == InvitationType.Employee ? "team member" : "customer portal user",
            RoleDisplayName = InvitationMapper.GetRoleDisplayName(invitation.TenantRole),
            AcceptUrl = acceptUrl,
            ExpiresAt = invitation.ExpiresAt.ToString("MMMM dd, yyyy")
        };

        var subject = $"Reminder: You're invited to join {companyName}";
        var body = await emailTemplateService.RenderAsync("InvitationReminder", model);

        return await emailSender.SendEmailAsync(invitation.Email, subject, body);
    }
}
