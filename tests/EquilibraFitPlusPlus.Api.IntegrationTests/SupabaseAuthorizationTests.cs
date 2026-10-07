using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace EquilibraFitPlusPlus.Api.IntegrationTests;

// WebApplicationFactory hosts share Program's static Serilog bootstrap logger.
[Collection("API hosts")]
public sealed class SupabaseAuthorizationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ForgedOwnerClaims_ShouldNotReadOrDeleteAnotherUsersWorkout(bool differentTenant)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        await client.GetAsync("/api/v1/admin/usuarios");
        Guid owner = Guid.NewGuid(), tenant, workoutId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>();
            tenant = (await db.Usuarios.SingleAsync()).TenantId;
            if (differentTenant)
            {
                tenant = Guid.NewGuid();
                db.Tenants.Add(new Tenant { Id = tenant, Nome = "Other tenant", Tipo = "Individual" });
            }
            db.Usuarios.Add(new Usuario { Id = owner, TenantId = tenant, IdentityUserId = owner,
                Nome = "Other user", Email = $"{owner:N}@example.test" });
            var workout = new TreinoUsuario { TenantId = tenant, UsuarioId = owner, Nome = "Private workout",
                Objetivo = "Hipertrofia", FrequenciaSemanal = 3, Versao = 1,
                DataInicio = new DateOnly(2026, 10, 1), DuracaoSemanas = 6, Fase = "Base", Ativo = true };
            db.Set<TreinoUsuario>().Add(workout);
            workoutId = workout.Id;
            await db.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(tenant: tenant, owner: owner));
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/treinos/{workoutId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync($"/api/v1/treinos/{workoutId}")).StatusCode);
        using var check = factory.Services.CreateScope();
        var saved = await check.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>().Set<TreinoUsuario>().SingleAsync();
        Assert.Null(saved.ExcluidoEm);
    }

    [Fact]
    public async Task Recovery_ShouldVerifyWithProviderAndRevokeExistingSessions()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        await client.GetAsync("/api/v1/admin/usuarios");
        client.DefaultRequestHeaders.Authorization = null;
        using var reset = await client.PostAsJsonAsync("/api/v1/auth/recuperar/confirmar", new { tokenHash = "valid-test-link", senha = "Test-password-only!" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(new[] { "/auth/v1/verify", "/auth/v1/user", "/auth/v1/logout" }, factory.Provider.Paths);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/usuarios")).StatusCode);
    }

    [Fact]
    public async Task GlobalRevocation_ShouldRejectPreviouslyUnseenSessionAndAllowNewSignIn()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        await client.GetAsync("/api/v1/admin/usuarios");
        var cutoff = DateTimeOffset.UtcNow;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>();
            (await db.Usuarios.SingleAsync()).SessoesRevogadasAntesDe = cutoff;
            await db.SaveChangesAsync();
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(session: Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/usuarios")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(session: Guid.NewGuid(), issuedAt: cutoff.AddSeconds(2)));
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/admin/usuarios")).StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token(session: Guid.NewGuid(),
            issuedAt: cutoff.AddSeconds(2), authenticatedAt: cutoff.AddHours(-1)));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/usuarios")).StatusCode);
    }

    [Fact]
    public async Task Recovery_ShouldRejectInvalidLinksAndShortPasswords()
    {
        using var factory = new ApiFactory();
        factory.Provider.AcceptRecovery = false;
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/recuperar/confirmar",
            new { tokenHash = "expired-test-link", senha = "Test-password-only!" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/recuperar/confirmar",
            new { tokenHash = "link", senha = "short" })).StatusCode);
        Assert.Single(factory.Provider.Paths);
    }

    [Fact]
    public async Task SignedUserToken_ShouldNotGrantForgedAdminPermissions()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        var response = await client.GetAsync("/api/v1/admin/usuarios");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>();
        Assert.Equal(factory.Subject, (await db.Usuarios.SingleAsync()).IdentityUserId);
        Assert.Equal(EquilibraFitPlusPlus.Domain.Enums.UsuarioRole.Usuario, (await db.Usuarios.SingleAsync()).Role);
    }

    [Fact]
    public async Task RevokedSession_ShouldRejectTheNextRequestWithTheSameSessionId()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        await client.GetAsync("/api/v1/admin/usuarios");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>();
            (await db.AuthSessions.SingleAsync()).RevogadoEm = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
        var response = await client.GetAsync("/api/v1/admin/usuarios");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongIssuerOrExpiredToken_ShouldBeUnauthorized()
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        foreach (var token in new[] { factory.Token(issuer: "https://other.example.test/auth/v1"), factory.Token(expired: true) })
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/admin/usuarios")).StatusCode);
        }
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private const string Issuer = "https://supabase.example.test/auth/v1";
        private readonly RSA _rsa = RSA.Create(2048);
        private readonly Guid _session = Guid.NewGuid();
        private readonly string _database = Path.Combine(Path.GetTempPath(), $"equilibrafit-pp-test-{Guid.NewGuid():N}.db");
        internal Guid Subject { get; } = Guid.NewGuid();
        internal ProviderHandler Provider { get; } = new();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("SUPABASE_URL", "https://supabase.example.test");
            builder.UseSetting("SUPABASE_PUBLISHABLE_KEY", "public-test-placeholder");
            builder.UseSetting("DATABASE_PROVIDER", "Sqlite");
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_database}");
            builder.ConfigureTestServices(services =>
            {
                Provider.Subject = Subject;
                services.AddSingleton<IHttpClientFactory>(new ProviderClients(Provider));
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var config = new OpenIdConnectConfiguration { Issuer = Issuer };
                    config.SigningKeys.Add(new RsaSecurityKey(_rsa) { KeyId = "test" });
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(config);
                });
            });
        }
        internal string Token(string? issuer = null, bool expired = false, Guid? tenant = null, Guid? owner = null,
            Guid? session = null, DateTimeOffset? issuedAt = null, DateTimeOffset? authenticatedAt = null)
        {
            var credentials = new SigningCredentials(new RsaSecurityKey(_rsa) { KeyId = "test" }, SecurityAlgorithms.RsaSha256);
            var token = new JwtSecurityToken(issuer ?? Issuer, "authenticated",
                [new Claim("sub", Subject.ToString()), new Claim("session_id", (session ?? _session).ToString()),
                 new Claim("iat", (issuedAt ?? DateTimeOffset.UtcNow.AddHours(-1)).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                 new Claim("amr", System.Text.Json.JsonSerializer.Serialize(new[] { new { method = "password",
                     timestamp = (authenticatedAt ?? issuedAt ?? DateTimeOffset.UtcNow.AddHours(-1)).ToUnixTimeSeconds() } }), JsonClaimValueTypes.JsonArray),
                 new Claim("email", $"{Subject:N}@example.test"), new Claim(ClaimTypes.Role, "Administrador"),
                 new Claim("tenant_id", (tenant ?? Guid.NewGuid()).ToString()), new Claim("usuario_id", (owner ?? Guid.NewGuid()).ToString())],
                DateTime.UtcNow.AddHours(-2), expired ? DateTime.UtcNow.AddHours(-1) : DateTime.UtcNow.AddHours(1), credentials);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) _rsa.Dispose();
        }
    }

    private sealed class ProviderClients(ProviderHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class ProviderHandler : HttpMessageHandler
    {
        internal Guid Subject { get; set; }
        internal bool AcceptRecovery { get; set; } = true;
        internal List<string> Paths { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Paths.Add(request.RequestUri!.AbsolutePath);
            if (request.RequestUri.AbsolutePath.EndsWith("/verify"))
                return Task.FromResult(new HttpResponseMessage(AcceptRecovery ? HttpStatusCode.OK : HttpStatusCode.BadRequest)
                { Content = JsonContent.Create(new { access_token = "provider-recovery-token-test-only", user = new { id = Subject } }) });
            Assert.Equal("provider-recovery-token-test-only", request.Headers.Authorization?.Parameter);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { }) });
        }
    }
}
