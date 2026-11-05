using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Email.Models;
using Trackwise.Application.Abstractions.Email;
using Trackwise.Application.Utilities;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Options;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Identity.Roles;
using Trackwise.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Trackwise.Application.Abstractions.Features;
using Trackwise.Application.Abstractions.Tenancy;

namespace Trackwise.Application.Modules.IdentityAccess.Tenants.Commands;

internal sealed class CreateTenantHandler(
    ITenantDatabaseService tenantDatabase,
    IMasterUnitOfWork masterUow,
    ITenantUnitOfWork tenantUow,
    IFeatureService featureService,
    UserManager<User> userManager,
    IEmailSender emailSender,
    IEmailTemplateService emailTemplateService,
    IOptions<IdentityServerOptions> identityServerOptions)
    : IAppRequestHandler<CreateTenantCommand, Result>
{
    public async Task<Result> Handle(CreateTenantCommand req, CancellationToken ct)
    {
        var tenantName = req.Name.Trim().ToLower();

        var tenant = new Tenant
        {
            Name = tenantName,
            CompanyName = req.CompanyName,
            DotNumber = req.DotNumber,
            CompanyAddress = req.CompanyAddress,
            BillingEmail = req.BillingEmail!,
            ConnectionString = tenantDatabase.GenerateConnectionString(tenantName),
            Settings = { OperatingMode = req.OperatingMode ?? OperatingMode.Fleet },
            // New tenants start subscription-less so they're usable immediately
            // (admin-led onboarding/impersonation). The subscription check only
            // enforces when IsSubscriptionRequired is true; an admin assigns a
            // subscription later via the Subscriptions page.
            IsSubscriptionRequired = false
        };
        tenant.Settings.ApplyRegionDefaults(req.Region ?? Region.US);

        var existingTenant = await masterUow.Repository<Tenant>().GetAsync(i => i.Name == tenant.Name, ct);

        if (existingTenant is not null)
        {
            return Result.Fail($"Tenant name '{tenant.Name}' is already taken, please chose another name");
        }

        var created = await tenantDatabase.CreateDatabaseAsync(tenant.ConnectionString);
        if (!created)
        {
            return Result.Fail("Could not create the tenant's database");
        }

        await masterUow.Repository<Tenant>().AddAsync(tenant, ct);
        await masterUow.SaveChangesAsync(ct);

        // Initialize feature configurations for the new tenant based on defaults
        await featureService.InitializeFeaturesForTenantAsync(tenant.Id);

        // Create owner account and send welcome email
        var ownerResult = await CreateOwnerAccountAsync(req, tenant);
        if (!ownerResult.IsSuccess)
        {
            return ownerResult;
        }

        return Result.Ok();
    }

    private async Task<Result> CreateOwnerAccountAsync(CreateTenantCommand req, Tenant tenant)
    {
        var existingUser = await userManager.FindByEmailAsync(req.OwnerEmail);
        User owner;

        if (existingUser is not null)
        {
            if (existingUser.TenantId is not null && existingUser.TenantId != tenant.Id)
            {
                return Result.Fail($"A user with email '{req.OwnerEmail}' already belongs to another tenant");
            }

            // Assign existing unassigned user to this tenant
            existingUser.TenantId = tenant.Id;
            existingUser.FirstName = req.OwnerFirstName;
            existingUser.LastName = req.OwnerLastName;
            await userManager.UpdateAsync(existingUser);
            owner = existingUser;
            await SendWelcomeEmailAsync(owner, tenant);
        }
        else
        {
            var temporaryPassword = TokenGenerator.GenerateSecureToken(12);
            owner = new User
            {
                UserName = req.OwnerEmail,
                Email = req.OwnerEmail,
                FirstName = req.OwnerFirstName,
                LastName = req.OwnerLastName,
                EmailConfirmed = true,
                TenantId = tenant.Id
            };

            var createResult = await userManager.CreateAsync(owner, temporaryPassword);
            if (!createResult.Succeeded)
            {
                return Result.Fail(
                    $"Failed to create owner account: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            await SendWelcomeEmailAsync(owner, tenant, temporaryPassword);
        }

        // Create employee record in tenant database with Owner role
        await tenantUow.SetCurrentTenantByIdAsync(tenant.Id);

        var ownerRole = await tenantUow.Repository<TenantRole>()
            .GetAsync(r => r.Name == TenantRoles.Owner);

        var employee = Employee.CreateEmployeeFromUser(owner);
        if (ownerRole is not null)
        {
            employee.Role = ownerRole;
        }

        await tenantUow.Repository<Employee>().AddAsync(employee);
        await tenantUow.SaveChangesAsync();

        return Result.Ok();
    }

    private async Task SendWelcomeEmailAsync(User owner, Tenant tenant, string? temporaryPassword = null)
    {
        var loginUrl = $"{identityServerOptions.Value.UserFacingAuthority}/Account/Login";
        var companyName = tenant.CompanyName ?? tenant.Name;

        var model = new TenantWelcomeEmailModel
        {
            OwnerName = $"{owner.FirstName} {owner.LastName}",
            CompanyName = companyName,
            Email = owner.Email!,
            TemporaryPassword = temporaryPassword,
            LoginUrl = loginUrl
        };

        var subject = $"Welcome to TrackWise - {companyName}";
        var body = await emailTemplateService.RenderAsync("TenantWelcome", model);

        await emailSender.SendEmailAsync(owner.Email!, subject, body);
    }
}
