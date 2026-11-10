using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Email.Models;
using Trackwise.Application.Abstractions.Email;
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
using Trackwise.Application.Abstractions.CurrentUser;
using Trackwise.Application.Modules.IdentityAccess.Roles.Services;

namespace Trackwise.Application.Modules.IdentityAccess.Invitations.Commands;

internal sealed class CreateInvitationHandler(
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow,
    UserManager<User> userManager,
    IEmailSender emailSender,
    IEmailTemplateService emailTemplateService,
    ICurrentUserService currentUserService,
    IOptions<IdentityServerOptions> identityServerOptions,
    ITenantRoleAccess roleAccess)
    : IAppRequestHandler<CreateInvitationCommand, Result<InvitationDto>>
{
    public async Task<Result<InvitationDto>> Handle(CreateInvitationCommand req, CancellationToken ct)
    {
        var currentUserId = currentUserService.GetUserId();
        if (currentUserId is null)
        {
            return Result<InvitationDto>.Fail("User not authenticated.");
        }

        var currentUser = await userManager.FindByIdAsync(currentUserId.Value.ToString());
        if (currentUser is null)
        {
            return Result<InvitationDto>.Fail("Current user not found.");
        }

        var tenant = tenantUow.GetCurrentTenant();

        // Validate role assignment permissions - Owner can only be assigned by SuperAdmin/Admin
        if (req.TenantRole == TenantRoles.Owner)
        {
            var currentUserRoles = await userManager.GetRolesAsync(currentUser);
            var canAssignOwner = currentUserRoles.Contains(AppRoles.SuperAdmin) ||
                currentUserRoles.Contains(AppRoles.Admin);

            if (!canAssignOwner)
            {
                return Result<InvitationDto>.Fail("Only Super Admins and Admins can invite Owner users.");
            }
        }

        if (req.Type == InvitationType.Employee)
        {
            var role = await tenantUow.Repository<TenantRole>().GetAsync(r => r.Name == req.TenantRole, ct);
            if (role is null)
            {
                return Result<InvitationDto>.Fail($"Could not find role '{req.TenantRole}'");
            }

            // Inviting someone into a role is assigning it, so it obeys the same rules.
            if (await roleAccess.CanAssignAsync(null, role, ct) is { IsSuccess: false } refused)
            {
                return Result<InvitationDto>.Fail(refused.Error!);
            }
        }

        // Check for existing pending invitation
        var existingInvitation = await masterUow.Repository<Invitation>()
            .GetAsync(i => i.Email == req.Email
                        && i.TenantId == tenant.Id
                        && i.Status == InvitationStatus.Pending, ct);

        if (existingInvitation is not null)
        {
            return Result<InvitationDto>.Fail("An active invitation already exists for this email.");
        }

        // Check if user already exists and is already a tenant member
        var existingUser = await masterUow.Repository<User>()
            .GetAsync(u => u.Email == req.Email, ct);

        if (existingUser is not null)
        {
            var isAlreadyMember = await CheckExistingMembershipAsync(existingUser.Id, req.Type, ct);
            if (isAlreadyMember)
            {
                return Result<InvitationDto>.Fail("This user is already a member of your organization.");
            }
        }

        // For CustomerUser, validate customer exists
        string? customerName = null;
        if (req.Type == InvitationType.CustomerUser)
        {
            if (!req.CustomerId.HasValue)
            {
                return Result<InvitationDto>.Fail("CustomerId is required for customer user invitations.");
            }

            var customer = await tenantUow.Repository<Customer>().GetByIdAsync(req.CustomerId.Value, ct);
            if (customer is null)
            {
                return Result<InvitationDto>.Fail("Customer not found.");
            }
            customerName = customer.Name;
        }

        var invitation = new Invitation
        {
            Email = req.Email,
            Token = TokenGenerator.GenerateSecureToken(64),
            TenantId = tenant.Id,
            Type = req.Type,
            TenantRole = req.TenantRole,
            CustomerId = req.CustomerId,
            ExpiresAt = DateTime.UtcNow.AddDays(req.ExpirationDays),
            InvitedByUserId = currentUserId.Value,
            PersonalMessage = req.PersonalMessage,
            LastSentAt = DateTime.UtcNow
        };

        // Sent before saving so a failed send leaves no pending invitation blocking the retry.
        if (!await SendInvitationEmailAsync(invitation, tenant, currentUser.GetFullName()))
        {
            return Result<InvitationDto>.Fail($"Could not send the invitation email to {req.Email}. Please try again.");
        }

        await masterUow.Repository<Invitation>().AddAsync(invitation, ct);
        await masterUow.SaveChangesAsync(ct);

        var dto = invitation.ToDto(
            tenant.CompanyName ?? tenant.Name,
            currentUser.GetFullName(),
            customerName);

        return Result<InvitationDto>.Ok(dto);
    }

    private async Task<bool> CheckExistingMembershipAsync(Guid userId, InvitationType type, CancellationToken ct)
    {
        if (type == InvitationType.Employee)
        {
            var employee = await tenantUow.Repository<Employee>().GetByIdAsync(userId, ct);
            return employee is not null;
        }
        else
        {
            var customerUser = await tenantUow.Repository<CustomerUser>()
                .GetAsync(cu => cu.UserId == userId, ct);
            return customerUser is not null;
        }
    }

    private async Task<bool> SendInvitationEmailAsync(Invitation invitation, Tenant tenant, string invitedByName)
    {
        var acceptUrl = $"{identityServerOptions.Value.UserFacingAuthority}/Account/AcceptInvitation?token={invitation.Token}";
        var companyName = tenant.CompanyName ?? tenant.Name;

        var model = new InvitationEmailModel
        {
            InvitedByName = invitedByName,
            CompanyName = companyName,
            TypeLabel = invitation.Type == InvitationType.Employee ? "team member" : "customer portal user",
            RoleDisplayName = InvitationMapper.GetRoleDisplayName(invitation.TenantRole),
            PersonalMessage = invitation.PersonalMessage,
            AcceptUrl = acceptUrl,
            ExpiresAt = invitation.ExpiresAt.ToString("MMMM dd, yyyy")
        };

        var subject = $"You're invited to join {companyName}";
        var body = await emailTemplateService.RenderAsync("Invitation", model);

        return await emailSender.SendEmailAsync(invitation.Email, subject, body);
    }
}
