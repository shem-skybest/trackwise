using System.Threading.RateLimiting;
using Duende.IdentityServer;
using Trackwise.Application;
using Trackwise.Application.Modules.IdentityAccess.Users.Services;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Options;
using Trackwise.HostDefaults;
using Trackwise.IdentityServer.Services;
using Trackwise.Infrastructure.Communications;
using Trackwise.Infrastructure.Persistence;
using Trackwise.Infrastructure.Persistence.Builder;
using Trackwise.Infrastructure.Persistence.Data;
using Trackwise.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Serilog;
using Serilog.Extensions.Logging;

namespace Trackwise.IdentityServer;

internal static class Setup
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        var serilogLogger = new SerilogLoggerFactory(Log.Logger)
            .CreateLogger<IPersistenceInfrastructureBuilder>();

        // Configuration options
        services.Configure<ImpersonationOptions>(configuration.GetSection(ImpersonationOptions.SectionName));

        // Infrastructure layers
        services.AddCommunicationsInfrastructure(configuration);
        services.AddPersistenceInfrastructure(configuration)
            .UseLogger(serilogLogger)
            .AddMasterDatabase()
            .AddTenantDatabase()
            .AddIdentity(identityBuilder =>
            {
                identityBuilder
                    .AddSignInManager()
                    .AddClaimsPrincipalFactory<UserCustomClaimsFactory>()
                    .AddDefaultTokenProviders();
            });

        // Backs the signed download URL on the Manage Privacy page's data exports.
        services.AddStorageInfrastructure(configuration);

        // Application services used directly by Razor pages. Still not the full Application/MediatR
        // stack - just UserService (Manage Profile) and the Privacy slice (Manage Privacy).
        services.AddScoped<IUserService, UserService>();
        services.AddApplicationPrivacyServices();

        services.AddRazorPages();
        AddAuthSchemes(services, builder.Environment.IsDevelopment());

        // Explicit app name (previously unset). This invalidates cookies protected before this
        // change - a one-time forced re-login; there are no persisted grants to lose.
        services.AddTrackwiseDataProtection<MasterDbContext>("Trackwise.IdentityServer");

        // Real health probe: master DB connectivity (composes with AddHealthChecks() in TrackwiseHost).
        services.AddHealthChecks().AddDbContextCheck<MasterDbContext>("master-db");

        services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/
                options.EmitStaticAudienceClaim = true;

                // Matches the ApplicationScheme cookie below: SameSite=None requires Secure,
                // which local HTTP dev can't provide - browsers drop the cookie outright otherwise.
                // CookieSameSiteMode also covers idsrv.external, the cookie Google sign-in lands in;
                // left at None, ExternalLogin/Callback finds no cookie and throws.
                if (builder.Environment.IsDevelopment())
                {
                    options.Authentication.CheckSessionCookieSameSiteMode = SameSiteMode.Lax;
                    options.Authentication.CookieSameSiteMode = SameSiteMode.Lax;
                }
            })
            .AddInMemoryIdentityResources(Config.IdentityResources())
            .AddInMemoryApiScopes(Config.ApiScopes())
            .AddInMemoryApiResources(Config.ApiResources())
            .AddInMemoryClients(Config.Clients(configuration))
            .AddAspNetIdentity<User>()
            // Signing keys + refresh tokens in the master DB; without this both live in the
            // container and every redeploy invalidates all sessions
            .AddOperationalStore(options =>
            {
                DuendeOperationalStore.ConfigureStoreOptions(options);
                options.ConfigureDbContext = b => DuendeOperationalStore.ConfigureDbContext(
                    b, configuration.GetConnectionString("MasterDatabase"));
                options.EnableTokenCleanup = true;
                options.RemoveConsumedTokens = true;
            });

        services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.ClientId = configuration["Authentication:Google:ClientId"]!;
                options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
            });

        services.AddTrackwiseCors(
            "https://trackwise.skybest.com.ph", "https://admin.skybest.com.ph");

        // Rate limiting configuration
        services.AddRateLimiter(options =>
        {
            // Rate limit for login attempts per IP
            options.AddPolicy("login", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(15),
                        SegmentsPerWindow = 3,
                        QueueLimit = 0
                    }));

            // Rate limit for impersonation token validation
            options.AddIpFixedWindowPolicy("impersonation", 5, TimeSpan.FromMinutes(15));

            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Redirect("/Account/Login?error=TooManyAttempts");
                await Task.CompletedTask;
            };
        });

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseTrackwiseCors();

        app.UseRateLimiter();
        app.UseIdentityServer();
        app.UseAuthorization();
        app.MapRazorPages().RequireAuthorization();
        return app;
    }

    private static void AddAuthSchemes(IServiceCollection services, bool isDevelopment)
    {
        // Local dev runs Identity Server over plain HTTP (see launchSettings.json), so a
        // Secure-only cookie would never reach the browser and login would never persist.
        // SameSite=None is only valid paired with Secure - browsers silently drop it otherwise -
        // so both have to flip together, not just SecurePolicy.
        var cookieSecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        var cookieSameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None;

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddCookie(IdentityConstants.ApplicationScheme, o =>
            {
                o.LoginPath = new PathString("/Account/Login");
                o.Cookie.SameSite = cookieSameSite;
                o.Cookie.SecurePolicy = cookieSecurePolicy;
                o.Cookie.HttpOnly = true;
                o.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync
                };
            })
            .AddCookie(IdentityConstants.ExternalScheme, o =>
            {
                o.Cookie.Name = IdentityConstants.ExternalScheme;
                o.ExpireTimeSpan = TimeSpan.FromMinutes(5.0);
            })
            .AddCookie(IdentityConstants.TwoFactorRememberMeScheme, o =>
            {
                o.Cookie.Name = IdentityConstants.TwoFactorRememberMeScheme;
                o.Events = new CookieAuthenticationEvents
                {
                    OnValidatePrincipal = SecurityStampValidator.ValidateAsync<ITwoFactorSecurityStampValidator>
                };
            })
            .AddCookie(IdentityConstants.TwoFactorUserIdScheme, o =>
            {
                o.Cookie.Name = IdentityConstants.TwoFactorUserIdScheme;
                o.ExpireTimeSpan = TimeSpan.FromMinutes(5.0);
            });

        // AddAspNetIdentity<User>() (registered later, in ConfigureServices) reconfigures the
        // ApplicationScheme cookie's SameSite back to None. PostConfigure always runs after every
        // Configure callback regardless of registration order, so this is the only way to make our
        // Development override stick.
        services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, o =>
        {
            o.Cookie.SameSite = cookieSameSite;
            o.Cookie.SecurePolicy = cookieSecurePolicy;
        });
    }
}
