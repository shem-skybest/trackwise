using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Trackwise.API.Authorization;
using Trackwise.API.Converters;
using Trackwise.API.Extensions;
using Trackwise.API.Jobs;
using Trackwise.API.Middlewares;
using Trackwise.API.ModelBinders;
using Trackwise.Application;
using Trackwise.Application.Abstractions.Accounting;
using Trackwise.Application.Abstractions.AICopilot;
using Trackwise.Application.Abstractions.AIDispatch;
using Trackwise.Application.Abstractions.BackgroundJobs;
using Trackwise.Application.Abstractions.Negotiation;
using Trackwise.Application.Modules.Operations.FreightForwarding.Services;
using Trackwise.Domain.Options;
using Trackwise.HostDefaults;
using Trackwise.Infrastructure.AI;
using Trackwise.Infrastructure.Communications;
using Trackwise.Infrastructure.Communications.SignalR.Hubs;
using Trackwise.Infrastructure.Documents;
using Trackwise.Infrastructure.Integrations.Accounting;
using Trackwise.Infrastructure.Integrations.Eld;
using Trackwise.Infrastructure.Integrations.FuelCards;
using Trackwise.Infrastructure.Integrations.LoadBoard;
using Trackwise.Infrastructure.Payments;
using Trackwise.Infrastructure.Persistence;
using Trackwise.Infrastructure.Persistence.Builder;
using Trackwise.Infrastructure.Persistence.Data;
using Trackwise.Infrastructure.Persistence.Services.Accounting;
using Trackwise.Infrastructure.Routing;
using Trackwise.Infrastructure.Storage;
using Trackwise.Infrastructure.Tax;
using Trackwise.Infrastructure.Vin;
using Trackwise.McpServer;
using Trackwise.Shared.Models;
using Trackwise.TelegramBot;

namespace Trackwise.API;

internal static class Setup
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;


        var serilogLogger = new SerilogLoggerFactory(Log.Logger)
            .CreateLogger<IPersistenceInfrastructureBuilder>();

        // Application layers
        services.AddApplicationLayer();

        // Infrastructure layers
        services.AddCommunicationsInfrastructure(configuration);
        services.AddDocumentsInfrastructure();
        services.AddVinInfrastructure();
        services.AddEldIntegrations(configuration);
        services.AddLoadBoardIntegrations(configuration);
        services.AddAccountingIntegrations(configuration);
        services.AddFuelCardIntegrations(configuration);
        services.AddPaymentsInfrastructure(configuration);
        services.AddTaxInfrastructure(configuration);
        services.AddRoutingInfrastructure(configuration);
        services.AddAIInfrastructure(configuration);
        services.AddMcpServerInfrastructure();
        services.AddTelegramBotInfrastructure(configuration, builder.Environment.IsDevelopment());
        services.AddStorageInfrastructure(configuration);
        services.AddPersistenceInfrastructure(configuration)
            .UseLogger(serilogLogger)
            .AddMasterDatabase()
            .AddTenantDatabase()
            .AddIdentity();

        services.AddMemoryCache();
        services.Configure<FxRateOptions>(
            builder.Configuration.GetSection(FxRateOptions.SectionName));
        services.AddHttpClient<IFxRateService, FxRateService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("User-Agent", "Trackwise-Logistics-Platform");
        });
        // Backs secret-column encryption and the OAuth state protector. Stable app name + master-DB
        // key ring so ciphertext survives restarts and works across API instances.
        services.AddTrackwiseDataProtection<MasterDbContext>("Trackwise");

        // Real health probe: master DB connectivity (composes with AddHealthChecks() in TrackwiseHost).
        services.AddHealthChecks().AddDbContextCheck<MasterDbContext>("master-db");

        // Registered here (not the shared persistence builder) since it needs Data Protection -
        // keeps hosts without it, e.g. the DB migrator, constructible.
        services.AddScoped<IOAuthStateProtector, OAuthStateProtector>();

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddAuthorization();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.Configure<ImpersonationOptions>(configuration.GetSection(ImpersonationOptions.SectionName));

        // Rate limiting configuration
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Strict rate limit for impersonation endpoint
            options.AddIpFixedWindowPolicy("impersonation", 5, TimeSpan.FromMinutes(15));

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { error = "Too many requests. Please try again later." },
                    cancellationToken);
            };
        });

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();

        services.AddScoped<IBackgroundJobRunner<AIDispatchTurnRequest>, HangfireAIDispatchTurnRunner>();
        services.AddScoped<IDelayedBackgroundJobRunner<NegotiationWakeRequest>, HangfireNegotiationWakeRunner>();
        services.AddScoped<IBackgroundJobRunner<AICopilotTurnRequest>, HangfireAICopilotTurnRunner>();
        services.AddScoped<ICommandEnqueuer, HangfireCommandEnqueuer>();
        services.AddHangfireServer();
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(c =>
                c.UseNpgsqlConnection(configuration.GetConnectionString("MasterDatabase"))));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Bind supplies RequireHttpsMetadata when configured; it otherwise stays true,
                // so only Development relaxes it.
                configuration.Bind("Jwt", options);

                if (builder.Environment.IsDevelopment() && configuration["Jwt:RequireHttpsMetadata"] is null)
                {
                    options.RequireHttpsMetadata = false;
                }

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidIssuers = configuration.GetSection("Jwt:ValidIssuers").Get<string[]>(),
                    ValidAudience = configuration["Jwt:Audience"]
                };

                // Browser websockets cannot set an Authorization header, so SignalR passes the
                // token as the access_token query parameter.
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) &&
                            context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };
            });

        services.AddControllers(configure =>
            {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

                configure.Filters.Add(new AuthorizeFilter(policy));

                // Add custom model binder for snake_case enum query parameters
                configure.ModelBinderProviders.Insert(0, new SnakeCaseEnumModelBinderProvider());
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                    new BadRequestObjectResult(Result.Fail(GetModelStateErrors(context.ModelState)));
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)
                );
                options.JsonSerializerOptions.Converters.Add(new Iso8601UtcDateTimeConverter());
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        services.AddTrackwiseCors(
            "https://trackwise.skybest.com.ph", "https://admin.skybest.com.ph");

        // Freght forwarding services
        builder.Services.AddFreightForwardingServices();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Serilog must wrap the exception handler so it logs AFTER error body is captured
        app.UseSerilogRequestLoggingWithErrorDetails();
        app.UseCustomExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseTrackwiseCors();

        app.UseLocalStorageStaticFiles();

        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireDashboardAuthorizationFilter(app.Environment.IsDevelopment())]
        });

        app.MapControllers();

        // MCP Server
        app.MapMcpEndpoint();

        // Telegram Bot
        app.MapTelegramWebhook();

        // SignalR Hubs
        app.MapHub<TrackingHub>("/hubs/tracking");
        app.MapHub<AIDispatchHub>("/hubs/ai-dispatch");
        app.MapHub<NotificationHub>("/hubs/notification");
        app.MapHub<ChatHub>("/hubs/chat");
        app.MapHub<CopilotHub>("/hubs/copilot");
        return app;
    }

    public static WebApplication ScheduleJobs(this WebApplication app)
    {
        PayrollGenerationJob.ScheduleJobs();
        EldSyncJob.ScheduleJobs();
        LoadBoardSyncJob.ScheduleJobs();
        AccountingSyncJob.ScheduleJobs();
        FuelCardSyncJob.ScheduleJobs();
        IftaQuarterCloseJob.ScheduleJobs();
        IftaFilingReminderJob.ScheduleJobs();
        MaintenanceReminderJob.ScheduleJobs();
        LicenseExpiryReminderJob.ScheduleJobs();
        VehiclePermitReminderJob.ScheduleJobs();
        InvitationExpiryJob.ScheduleJobs();
        DataExportProcessingJob.ScheduleJobs();
        DataDeletionJob.ScheduleJobs();
        DataRetentionJob.ScheduleJobs();
        DataExportExpiryJob.ScheduleJobs();
        WebhookEventCleanupJob.ScheduleJobs();
        AIDispatchPolicyLearningJob.ScheduleJobs();
        NegotiationExpirySweepJob.ScheduleJobs();

        // Remove old stale dispatch agent job if it exists
        RecurringJob.RemoveIfExists("ai-dispatch");
        return app;
    }

    private static string GetModelStateErrors(ModelStateDictionary modelState)
    {
        var errors = new StringBuilder();
        foreach (var error in modelState.Values.SelectMany(modelStateValue => modelStateValue.Errors))
        {
            errors.Append($"{error.ErrorMessage} ");
        }

        return errors.ToString();
    }
}
