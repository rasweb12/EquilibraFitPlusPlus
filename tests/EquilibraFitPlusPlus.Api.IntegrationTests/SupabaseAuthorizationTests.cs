using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Infrastructure.AiCoach;
using EquilibraFitPlusPlus.Contracts.Common;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

    [Theory]
    [InlineData(false, 502, false, 502, AiServiceErrors.BadGateway)]
    [InlineData(true, 502, false, 502, AiServiceErrors.BadGateway)]
    [InlineData(false, 401, false, 502, AiServiceErrors.Authentication)]
    [InlineData(true, 401, false, 502, AiServiceErrors.Authentication)]
    [InlineData(false, 200, false, 502, AiServiceErrors.InvalidResponse)]
    [InlineData(true, 200, false, 502, AiServiceErrors.InvalidResponse)]
    [InlineData(false, 503, false, 503, AiServiceErrors.Unavailable)]
    [InlineData(true, 503, false, 503, AiServiceErrors.Unavailable)]
    [InlineData(false, 200, true, 504, AiServiceErrors.Timeout)]
    [InlineData(true, 200, true, 504, AiServiceErrors.Timeout)]
    public async Task AiTransportFailure_ShouldReturnSafeStatusAndNeverPersistFallback(bool meal, int upstreamStatus, bool delay, int expectedStatus, string code)
    {
        using var factory = new ApiFactory();
        factory.AiProvider.Status = (HttpStatusCode)upstreamStatus;
        factory.AiProvider.Delay = delay;
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        string correlation = Guid.NewGuid().ToString();
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlation);
        using var response = meal
            ? await client.PostAsJsonAsync("/api/v1/alimentacao/reconhecer-refeicao", new { imageBase64 = "aW1hZ2U=", tipoRefeicao = "Almoco" })
            : await client.PostAsJsonAsync("/api/v1/ia/coach/mensagens", new { mensagem = "Como ajustar a rotina?" });
        Assert.Equal(expectedStatus, (int)response.StatusCode);
        var error = (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!;
        Assert.Equal(code, error.Code);
        Assert.DoesNotContain("clinico", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-user-and-key", error.Message);
        Assert.Equal(correlation, response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal(correlation, factory.AiProvider.Correlation);
        Assert.Equal(1, factory.AiProvider.Calls);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>();
        Assert.Empty(await db.Set<ChatSession>().ToListAsync());
        Assert.Empty(await db.Set<AnaliseRefeicaoImagem>().ToListAsync());
    }

    /// <summary>Authenticated Coach requests keep the selected provider and returned model.</summary>
    [Theory]
    [InlineData("openai", "gpt-4.1-mini")]
    [InlineData("gemini", "gemini-2.5-flash")]
    public async Task Coach_ShouldForwardAuthenticatedProviderChoice(string provider, string model)
    {
        using var factory = new ApiFactory();
        factory.AiProvider.Status = HttpStatusCode.OK;
        factory.AiProvider.Body = System.Text.Json.JsonSerializer.Serialize(new { conteudo = "Podemos seguir com calma.", modelo = model });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        using var response = await client.PostAsJsonAsync("/api/v1/ia/coach/mensagens", new { mensagem = "Como ajustar a rotina?", provedor = provider });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(provider, factory.AiProvider.RequestProvider);
        Assert.Equal(1, factory.AiProvider.Calls);
        using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(model, body.RootElement.GetProperty("mensagemCoach").GetProperty("modeloIa").GetString());
    }

    /// <summary>Invalid selections are rejected before any upstream call or conversation write.</summary>
    [Theory]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData("OpenAI")]
    [InlineData("https://untrusted.test")]
    public async Task Coach_ShouldRejectInvalidProviderBeforeCallingAi(string provider)
    {
        using var factory = new ApiFactory();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        using var response = await client.PostAsJsonAsync("/api/v1/ia/coach/mensagens", new { mensagem = "Como ajustar a rotina?", provedor = provider });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, factory.AiProvider.Calls);
        using var scope = factory.Services.CreateScope();
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>().Set<ChatSession>().ToListAsync());
    }

    [Fact]
    public async Task Coach_ShouldPreserveClinicalGuardAndSuccessfulRulesFallback()
    {
        using var factory = new ApiFactory();
        factory.AiProvider.Status = HttpStatusCode.OK;
        factory.AiProvider.Body = """{"conteudo":"Prescrevo um tratamento.","modelo":"fixture"}""";
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        using var blocked = await client.PostAsJsonAsync("/api/v1/ia/coach/mensagens", new { mensagem = "Como ajustar a rotina?" });
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal("ia.coach_risco_clinico", (await blocked.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Code);
        factory.AiProvider.Body = """{"conteudo":"Sem problemas. Vamos ajustar a rotina em pequenos passos.","modelo":"equilibrafit-coach-rules-v1","fallback_used":true}""";
        using var accepted = await client.PostAsJsonAsync("/api/v1/ia/coach/mensagens", new { mensagem = "Como ajustar a rotina?" });
        Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        using var scope = factory.Services.CreateScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>().Set<ChatSession>().ToListAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Coach_ApprovedDisclaimerShouldNotTriggerClinicalBlock(bool unsafeClaim)
    {
        using var factory = new ApiFactory();
        factory.AiProvider.Status = HttpStatusCode.OK;
        string content = "O Coach IA orienta e educa, mas não substitui médicos, nutricionistas ou profissionais habilitados.";
        if (unsafeClaim) content = "Este Coach substitui seu médico. " + content;
        factory.AiProvider.Body = System.Text.Json.JsonSerializer.Serialize(new { conteudo = content, modelo = "fixture" });
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", factory.Token());
        using var response = await client.PostAsJsonAsync("/api/v1/ia/coach/mensagens", new { mensagem = "Como ajustar a rotina?" });
        Assert.Equal(unsafeClaim ? HttpStatusCode.BadRequest : HttpStatusCode.OK, response.StatusCode);
        if (unsafeClaim)
            Assert.Equal("ia.coach_risco_clinico", (await response.Content.ReadFromJsonAsync<ApiErrorResponse>())!.Code);
    }

    private sealed class ApiFactory : WebApplicationFactory<Program>
    {
        private const string Issuer = "https://supabase.example.test/auth/v1";
        private readonly RSA _rsa = RSA.Create(2048);
        private readonly Guid _session = Guid.NewGuid();
        private readonly string _database = Path.Combine(Path.GetTempPath(), $"equilibrafit-pp-test-{Guid.NewGuid():N}.db");
        internal Guid Subject { get; } = Guid.NewGuid();
        internal ProviderHandler Provider { get; } = new();
        internal AiProviderHandler AiProvider { get; } = new();
        private HttpClient? _aiHttp;
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
                services.AddSingleton<IAiCoachClient>(provider =>
                {
                    var handler = new AiCorrelationHandler(provider.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>())
                        { InnerHandler = AiProvider };
                    _aiHttp = new HttpClient(handler) { BaseAddress = new Uri("https://ai.example.test"), Timeout = TimeSpan.FromMilliseconds(100) };
                    return new AiCoachHttpClient(_aiHttp, Options.Create(new AiCoachOptions()), provider.GetRequiredService<ILogger<AiCoachHttpClient>>());
                });
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
            if (disposing)
            {
                _rsa.Dispose();
                _aiHttp?.Dispose();
            }
        }
    }

    private sealed class ProviderClients(ProviderHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class AiProviderHandler : HttpMessageHandler
    {
        internal HttpStatusCode Status { get; set; } = HttpStatusCode.BadGateway;
        internal string Body { get; set; } = "<html>private-user-and-key</html>";
        internal bool Delay { get; set; }
        internal int Calls { get; private set; }
        internal string? Correlation { get; private set; }
        internal string? RequestProvider { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            Correlation = request.Headers.GetValues("X-Correlation-ID").Single();
            using var payload = System.Text.Json.JsonDocument.Parse(await request.Content!.ReadAsStringAsync(ct));
            RequestProvider = payload.RootElement.TryGetProperty("provider", out var provider) ? provider.GetString() : null;
            if (Delay) await Task.Delay(System.Threading.Timeout.Infinite, ct);
            var response = new HttpResponseMessage(Status)
                { Content = new StringContent(Body, System.Text.Encoding.UTF8, Status == HttpStatusCode.OK ? "application/json" : "text/html") };
            response.Headers.Add("X-Correlation-ID", Correlation);
            return response;
        }
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
