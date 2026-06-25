using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace StudentPortal.Api.Middleware;

public static class KeycloakAuthExtensions
{
    public static IServiceCollection AddKeycloakAuth(this IServiceCollection services, IConfiguration config)
    {
        var authority = config["Keycloak:Authority"];
        var audience = config["Keycloak:Audience"];

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.Configuration = new OpenIdConnectConfiguration
            {
                Issuer = "http://localhost:8080/realms/student-portal"
            };

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                SignatureValidator = (token, parameters) => new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(token)
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"❌ Auth Failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    // Use context.Principal.Claims directly — works with both JwtSecurityToken
                    // (legacy) and JsonWebToken (.NET 7+ default). Casting to JwtSecurityToken
                    // silently fails in .NET 7+ which uses JsonWebToken internally.
                    var principal = context.Principal;
                    if (principal == null) return Task.CompletedTask;

                    var realmAccessClaim = principal.Claims
                        .FirstOrDefault(c => c.Type == "realm_access");

                    if (realmAccessClaim != null)
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(realmAccessClaim.Value);
                            if (doc.RootElement.TryGetProperty("roles", out var rolesElement) &&
                                rolesElement.ValueKind == JsonValueKind.Array)
                            {
                                var claimsIdentity = principal.Identity as ClaimsIdentity;
                                if (claimsIdentity != null)
                                {
                                    foreach (var role in rolesElement.EnumerateArray())
                                    {
                                        var roleName = role.GetString();
                                        if (!string.IsNullOrEmpty(roleName))
                                        {
                                            claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                                        }
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Silently ignore claim parse errors
                        }
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}
