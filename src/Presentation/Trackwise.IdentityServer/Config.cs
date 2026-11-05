using Duende.IdentityModel;
using Duende.IdentityServer.Models;

using Trackwise.Shared.Identity.Claims;

namespace Trackwise.IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources()
    {
        // Job title rides the standard profile scope every portal already requests, so it needs no
        // new scope in each environment's client config.
        var profile = new IdentityResources.Profile();
        profile.UserClaims.Add(CustomClaimTypes.JobTitle);

        return
        [
            new IdentityResources.OpenId(),
            profile,
            new IdentityResource
            {
                Name = "roles",
                DisplayName = "Identity roles",
                UserClaims = {
                    CustomClaimTypes.Role
                }
            },
            new IdentityResource
            {
                Name = "tenant",
                DisplayName = "Tenant ID",
                UserClaims = {
                    CustomClaimTypes.Tenant
                }
            }
        ];
    }

    public static IEnumerable<ApiScope> ApiScopes()
    {
        return
        [
            new ApiScope
            {
                Name = "trackwise.api.admin",
                DisplayName = "Trackwise Admin API",
                UserClaims = {
                    JwtClaimTypes.GivenName,
                    JwtClaimTypes.FamilyName,
                    CustomClaimTypes.Role
                }
            },
            new ApiScope
            {
                Name = "trackwise.api.tenant",
                DisplayName = "Trackwise Tenant API",
                UserClaims = {
                    JwtClaimTypes.GivenName,
                    JwtClaimTypes.FamilyName,
                    CustomClaimTypes.Role,
                    CustomClaimTypes.Tenant
                }
            }
        ];
    }

    public static IEnumerable<ApiResource> ApiResources()
    {
        return
        [
            new ApiResource
            {
                Name = "trackwise.api",
                DisplayName = "Trackwise API",
                Scopes = {
                    "trackwise.api.admin",
                    "trackwise.api.tenant"
                },
                UserClaims = {
                    JwtClaimTypes.GivenName,
                    JwtClaimTypes.FamilyName,
                    CustomClaimTypes.Role,
                    CustomClaimTypes.Tenant
                }
            }
        ];
    }

    public static IEnumerable<Client> Clients(IConfiguration configuration)
    {
        //var a = "Super secret key 1".Sha256();
        //var b = "Super secret key 2".Sha256();
        return configuration.GetSection("IdentityServer:Clients").Get<Client[]>();
    }
}
