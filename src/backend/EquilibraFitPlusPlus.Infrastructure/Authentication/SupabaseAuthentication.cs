using System.Security.Claims;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace EquilibraFitPlusPlus.Infrastructure.Authentication;

public static class SupabaseAuthentication
{
    public static IServiceCollection AddSupabaseAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var supabase = SupabaseOptions.FromConfiguration(configuration);
        services.AddSingleton(supabase);
        string issuer = new Uri(supabase.Url, "auth/v1").AbsoluteUri;
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.SaveToken = false;
            options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                issuer + "/.well-known/jwks.json", new JwksRetriever(issuer),
                new HttpDocumentRetriever { RequireHttps = true });
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = issuer,
                ValidateAudience = true, ValidAudience = "authenticated",
                ValidateLifetime = true, ValidateIssuerSigningKey = true,
                RequireSignedTokens = true, RequireExpirationTime = true,
                ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.RsaSha256],
                RoleClaimType = ClaimTypes.Role,
                NameClaimType = "sub", ClockSkew = TimeSpan.FromSeconds(30)
            };
            options.Events = new JwtBearerEvents { OnTokenValidated = ResolveApplicationContextAsync };
        });
        return services;
    }

    private static async Task ResolveApplicationContextAsync(TokenValidatedContext context)
    {
        if (!Guid.TryParse(context.Principal?.FindFirstValue("sub"), out var subject) ||
            !Guid.TryParse(context.Principal.FindFirstValue("session_id"), out var sessionId))
        {
            context.Fail("Invalid Supabase identity or session.");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<EquilibraFitPlusPlusDbContext>();
        var ct = context.HttpContext.RequestAborted;
        var user = await db.Usuarios.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.IdentityUserId == subject, ct);
        if (user is null)
        {
            // Only a verified Supabase token may provision a profile. Permissions come from the database.
            user = new Usuario
            {
                Id = subject, IdentityUserId = subject, TenantId = SeedData.DefaultTenantId,
                Nome = "Usuario", Email = context.Principal.FindFirstValue("email") ?? string.Empty
            };
            db.Usuarios.Add(user);
            try { await db.SaveChangesAsync(ct); }
            catch (PersistenceConflictException ex) when (ex.Code == "persistence.unique_constraint")
            {
                db.ChangeTracker.Clear();
                user = await db.Usuarios.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.IdentityUserId == subject, ct);
                if (user == null) { context.Fail("Profile could not be provisioned."); return; }
            }
        }
        if (user.ExcluidoEm is not null || user.Status != UsuarioStatus.Ativo)
        {
            context.Fail("Account is unavailable.");
            return;
        }

        if (user.SessoesRevogadasAntesDe is { } cutoff && SupabaseSessionPolicy.IsRevoked(context.Principal.Claims, cutoff))
        {
            context.Fail("Token predates global session revocation.");
            return;
        }

        var session = await db.AuthSessions.SingleOrDefaultAsync(x => x.SessionId == sessionId, ct);
        if (session is null)
        {
            session = new AuthSession { SessionId = sessionId, UsuarioId = user.Id, TenantId = user.TenantId };
            db.AuthSessions.Add(session);
            try { await db.SaveChangesAsync(ct); }
            catch (PersistenceConflictException ex) when (ex.Code == "persistence.unique_constraint")
            {
                db.ChangeTracker.Clear();
                session = await db.AuthSessions.SingleAsync(x => x.SessionId == sessionId, ct);
            }
        }
        if (session.UsuarioId != user.Id || session.TenantId != user.TenantId || session.RevogadoEm is not null)
        {
            context.Fail("Session was revoked.");
            return;
        }
        var identity = (ClaimsIdentity)context.Principal.Identity!;
        foreach (var claim in identity.FindAll("usuario_id").Concat(identity.FindAll("tenant_id"))
            .Concat(identity.FindAll(ClaimTypes.Role)).ToArray())
        {
            identity.RemoveClaim(claim);
        }
        identity.AddClaim(new Claim("usuario_id", user.Id.ToString()));
        identity.AddClaim(new Claim("tenant_id", user.TenantId.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
    }

    private sealed class JwksRetriever(string issuer) : IConfigurationRetriever<OpenIdConnectConfiguration>
    {
        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
            string address, IDocumentRetriever retriever, CancellationToken cancellationToken)
        {
            var document = await retriever.GetDocumentAsync(address, cancellationToken);
            var config = new OpenIdConnectConfiguration { Issuer = issuer };
            foreach (var key in new JsonWebKeySet(document).GetSigningKeys()) config.SigningKeys.Add(key);
            return config;
        }
    }
}
