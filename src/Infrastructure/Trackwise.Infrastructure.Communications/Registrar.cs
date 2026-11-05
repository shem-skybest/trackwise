using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Trackwise.Application.Abstractions.Email;
using Trackwise.Infrastructure.Communications.Captcha;
using Trackwise.Infrastructure.Communications.Email;
using Trackwise.Infrastructure.Communications.Services;
using Trackwise.Infrastructure.Communications.SignalR.Hubs;
using Trackwise.Infrastructure.Communications.SignalR.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Resend;
using Trackwise.Application.Abstractions.Captcha;
using Trackwise.Application.Abstractions.Notifications;
using Trackwise.Application.Abstractions.Realtime;
using Trackwise.Application.Abstractions.Routing;
using Trackwise.Application.Abstractions.AICopilot;
using Trackwise.Application.Abstractions.AIDispatch;

namespace Trackwise.Infrastructure.Communications;

public static class Registrar
{
    /// <summary>
    ///     Add communications infrastructure (SignalR, Email, Push Notifications).
    /// </summary>
    public static IServiceCollection AddCommunicationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Hub payloads must match the REST wire contract (snake_case string enums) - the generated
        // frontend types are string unions, so numeric enums silently fail every status comparison.
        services.AddSignalR().AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        });
        services.AddSingleton<TrackingHubContext>();
        services.AddSingleton<ChatHubContext>();

        // SignalR abstractions for Application layer
        services.AddScoped<IRealtimeMessagingService, SignalRMessagingService>();
        services.AddScoped<IRealtimeLiveTrackingService, SignalRLiveTrackingService>();
        services.AddScoped<IRealtimeNotificationService, SignalRNotificationService>();
        services.AddScoped<ITripTrackingService, SignalRTripTrackingService>();
        services.AddScoped<IAIDispatchBroadcastService, SignalRAIDispatchBroadcastService>();
        services.AddScoped<IAICopilotBroadcastService, SignalRAICopilotBroadcastService>();

        // Email services (Resend)
        services.Configure<ResendOptions>(configuration.GetSection(ResendOptions.SectionName));
        services.AddOptions();
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(o =>
        {
            o.ApiToken = configuration.GetSection(ResendOptions.SectionName)["ApiKey"]
                         ?? throw new InvalidOperationException("Resend:ApiKey is not configured");
        });
        services.AddTransient<IResend, ResendClient>();
        services.AddScoped<IEmailSender, ResendEmailSender>();
        services.AddScoped<IThreadedEmailSender, ResendThreadedEmailSender>();
        services.AddHttpClient<IInboundEmailReader, ResendInboundEmailReader>((sp, client) =>
        {
            var resendOptions = sp.GetRequiredService<IOptions<ResendOptions>>().Value;
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", resendOptions.ApiKey);
        });
        services.AddScoped<IInboundEmailWebhookVerifier, ResendWebhookVerifier>();
        services.AddSingleton<IEmailTemplateService, FluidEmailTemplateService>();

        // Push notifications
        services.AddScoped<INotificationService, NotificationService>();
        services.AddSingleton<IPushNotificationService, PushNotificationService>();

        // Google Recaptcha
        services.Configure<GoogleRecaptchaOptions>(configuration.GetSection(GoogleRecaptchaOptions.SectionName));
        services.AddSingleton<ICaptchaService, GoogleRecaptchaService>();

        return services;
    }
}
