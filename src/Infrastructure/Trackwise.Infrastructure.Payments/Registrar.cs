using Trackwise.Infrastructure.Payments.Stripe;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trackwise.Application.Abstractions.Payments;
using Trackwise.Application.Abstractions.Payments.Stripe;
using Trackwise.Application.Abstractions.Payments.Xendit;
using Trackwise.Infrastructure.Payments.Xendit;

namespace Trackwise.Infrastructure.Payments;

public static class Registrar
{
    /// <summary>
    ///     Add payment infrastructure (Stripe standard + Connect, Xendit).
    /// </summary>
    public static IServiceCollection AddPaymentsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuration
        services.Configure<StripeOptions>(
            configuration.GetSection(StripeOptions.SectionName));

        // Bootstraps Stripe.NET's static StripeConfiguration.ApiKey once at host startup so
        // every Stripe-using service (here and in Trackwise.Infrastructure.Tax) can rely on it
        // without repeating the init.
        services.AddHostedService<StripeApiKeyInitializer>();

        // Stripe services
        services.AddSingleton<IStripeService, StripeService>();
        services.AddSingleton<IStripeCustomerService, StripeCustomerService>();
        services.AddScoped<IStripeSubscriptionService, StripeSubscriptionService>();
        services.AddScoped<IStripePlanService, StripePlanService>();
        services.AddSingleton<IStripePaymentService, StripePaymentService>();
        services.AddSingleton<IStripeConnectService, StripeConnectService>();
        services.AddSingleton<IStripePortalService, StripePortalService>();
        services.AddScoped<IStripeUsageService, StripeUsageService>();
        services.AddSingleton<IStripeAddressMapper, StripeAddressMapper>();

        // Xendit (PH): keys are per tenant, so the client carries none of its own
        services.AddHttpClient<IXenditClient, XenditClient>(client =>
        {
            client.BaseAddress = new Uri(XenditClient.BaseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
