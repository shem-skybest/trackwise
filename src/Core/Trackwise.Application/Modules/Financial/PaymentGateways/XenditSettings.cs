using Microsoft.Extensions.Logging;
using Trackwise.Application.Abstractions;
using Trackwise.Application.Abstractions.Payments.Xendit;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Persistence;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Financial.PaymentGateways;

/// <summary>The company's Xendit connection, without the secrets.</summary>
public record GetXenditSettingsQuery : IQuery<Result<XenditSettingsDto>>;

/// <summary>
/// Connects or updates the company's Xendit account. On first save both values are required;
/// afterwards a blank value keeps the saved one, so staff can toggle <see cref="IsActive"/>
/// without re-pasting keys.
/// </summary>
public record SaveXenditSettingsCommand : ICommand<Result<XenditSettingsDto>>
{
    public string? SecretKey { get; init; }
    public string? CallbackToken { get; init; }
    public bool IsActive { get; init; } = true;
}

/// <summary>Disconnects Xendit: removes the saved keys. Recorded payments stay.</summary>
public record DeleteXenditSettingsCommand : ICommand<Result>;

internal sealed class GetXenditSettingsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<GetXenditSettingsQuery, Result<XenditSettingsDto>>
{
    public async Task<Result<XenditSettingsDto>> Handle(GetXenditSettingsQuery req, CancellationToken ct)
    {
        var config = await tenantUow.Repository<PaymentGatewayConfiguration>()
            .GetAsync(g => g.Gateway == PaymentGateway.Xendit, ct);
        return Result<XenditSettingsDto>.Ok(XenditSettingsMapping.ToDto(config));
    }
}

internal sealed class SaveXenditSettingsHandler(
    ITenantUnitOfWork tenantUow,
    IXenditClient xenditClient,
    ILogger<SaveXenditSettingsHandler> logger)
    : IAppRequestHandler<SaveXenditSettingsCommand, Result<XenditSettingsDto>>
{
    public async Task<Result<XenditSettingsDto>> Handle(SaveXenditSettingsCommand req, CancellationToken ct)
    {
        var secretKey = req.SecretKey?.Trim();
        var callbackToken = req.CallbackToken?.Trim();

        var repo = tenantUow.Repository<PaymentGatewayConfiguration>();
        var config = await repo.GetAsync(g => g.Gateway == PaymentGateway.Xendit, ct);

        if (config is null && (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(callbackToken)))
        {
            return Result<XenditSettingsDto>.Fail("Enter both the secret key and the callback verification token.");
        }

        if (!string.IsNullOrEmpty(secretKey))
        {
            if (!secretKey.StartsWith("xnd_", StringComparison.Ordinal))
            {
                return Result<XenditSettingsDto>.Fail(
                    "That doesn't look like a Xendit secret key. It starts with xnd_production_ or xnd_development_.");
            }

            if (secretKey.StartsWith("xnd_public_", StringComparison.Ordinal))
            {
                return Result<XenditSettingsDto>.Fail(
                    "That is the public key. Paste the secret key instead.");
            }

            var problem = await xenditClient.ValidateSecretKeyAsync(secretKey, ct);
            if (problem is not null)
            {
                return Result<XenditSettingsDto>.Fail(problem);
            }
        }

        if (config is null)
        {
            config = new PaymentGatewayConfiguration
            {
                Gateway = PaymentGateway.Xendit,
                SecretKey = secretKey!,
                CallbackToken = callbackToken!,
                IsActive = req.IsActive
            };
            await repo.AddAsync(config, ct);
        }
        else
        {
            if (!string.IsNullOrEmpty(secretKey))
            {
                config.SecretKey = secretKey;
            }

            if (!string.IsNullOrEmpty(callbackToken))
            {
                config.CallbackToken = callbackToken;
            }

            config.IsActive = req.IsActive;
            repo.Update(config);
        }

        await tenantUow.SaveChangesAsync(ct);
        logger.LogInformation(
            "Saved Xendit settings (active: {IsActive}, test mode: {IsTestMode})",
            config.IsActive, config.IsTestMode);

        return Result<XenditSettingsDto>.Ok(XenditSettingsMapping.ToDto(config));
    }
}

internal sealed class DeleteXenditSettingsHandler(ITenantUnitOfWork tenantUow)
    : IAppRequestHandler<DeleteXenditSettingsCommand, Result>
{
    public async Task<Result> Handle(DeleteXenditSettingsCommand req, CancellationToken ct)
    {
        var repo = tenantUow.Repository<PaymentGatewayConfiguration>();
        var config = await repo.GetAsync(g => g.Gateway == PaymentGateway.Xendit, ct);
        if (config is null)
        {
            return Result.Ok();
        }

        repo.Delete(config);
        await tenantUow.SaveChangesAsync(ct);
        return Result.Ok();
    }
}

internal static class XenditSettingsMapping
{
    public static XenditSettingsDto ToDto(PaymentGatewayConfiguration? config) => config is null
        ? new XenditSettingsDto()
        : new XenditSettingsDto
        {
            IsConfigured = true,
            IsActive = config.IsActive,
            IsTestMode = config.IsTestMode,
            SecretKeyHint = Hint(config.SecretKey)
        };

    private static string Hint(string key)
    {
        var prefix = key.StartsWith("xnd_production_", StringComparison.Ordinal) ? "xnd_production_"
            : key.StartsWith("xnd_development_", StringComparison.Ordinal) ? "xnd_development_"
            : "";
        return key.Length > prefix.Length + 4 ? $"{prefix}…{key[^4..]}" : prefix + "…";
    }
}
