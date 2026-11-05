using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Email.Models;
using Trackwise.Application.Abstractions.Email;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Options;
using Trackwise.Domain.Persistence;
using Trackwise.Shared.Models;
using Microsoft.Extensions.Options;

namespace Trackwise.Application.Modules.IdentityAccess.Tenants.Commands;

internal sealed class ResendTenantWelcomeHandler(
    IMasterUnitOfWork masterUow,
    IEmailSender emailSender,
    IEmailTemplateService emailTemplateService,
    IOptions<IdentityServerOptions> identityServerOptions)
    : IAppRequestHandler<ResendTenantWelcomeCommand, Result>
{
    public async Task<Result> Handle(ResendTenantWelcomeCommand req, CancellationToken ct)
    {
        var tenant = await masterUow.Repository<Tenant>().GetByIdAsync(req.TenantId, ct);

        if (tenant is null)
        {
            return Result.Fail("Tenant not found");
        }

        var owner = await masterUow.Repository<User>().GetAsync(u => u.TenantId == req.TenantId, ct);

        if (owner is null)
        {
            return Result.Fail("No owner found for this tenant");
        }

        var loginUrl = $"{identityServerOptions.Value.UserFacingAuthority}/Account/Login";
        var companyName = tenant.CompanyName ?? tenant.Name;

        var model = new TenantWelcomeEmailModel
        {
            OwnerName = $"{owner.FirstName} {owner.LastName}",
            CompanyName = companyName,
            Email = owner.Email!,
            LoginUrl = loginUrl
        };

        var subject = $"Welcome to TrackWise - {companyName}";
        var body = await emailTemplateService.RenderAsync("TenantWelcome", model);

        await emailSender.SendEmailAsync(owner.Email!, subject, body);

        return Result.Ok();
    }
}
