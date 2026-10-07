using System.Net;
using System.Net.Http.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Authentication;
using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Contracts.Common;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Infrastructure.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EquilibraFitPlusPlus.Api.IntegrationTests;

/// <summary>Exercise real auth handlers and HTTP error contracts with an isolated fake provider.</summary>
[Collection("API hosts")]
public sealed class AuthProviderFailureTests
{
    /// <summary>Auth transport failures are temporary, safe and never automatically retried.</summary>
    [Theory]
    [InlineData("cadastrar", "timeout", 504, "auth.provider_timeout")]
    [InlineData("login", "timeout", 504, "auth.provider_timeout")]
    [InlineData("refresh", "timeout", 504, "auth.provider_timeout")]
    [InlineData("cadastrar", "network", 503, "auth.provider_unavailable")]
    [InlineData("login", "network", 503, "auth.provider_unavailable")]
    [InlineData("refresh", "network", 503, "auth.provider_unavailable")]
    [InlineData("cadastrar", "gateway", 504, "auth.provider_timeout")]
    [InlineData("login", "gateway", 504, "auth.provider_timeout")]
    [InlineData("refresh", "gateway", 504, "auth.provider_timeout")]
    [InlineData("cadastrar", "unavailable", 503, "auth.provider_unavailable")]
    [InlineData("login", "unavailable", 503, "auth.provider_unavailable")]
    [InlineData("refresh", "unavailable", 503, "auth.provider_unavailable")]
    [InlineData("cadastrar", "invalid-json", 503, "auth.provider_unavailable")]
    [InlineData("login", "invalid-json", 503, "auth.provider_unavailable")]
    [InlineData("refresh", "invalid-json", 503, "auth.provider_unavailable")]
    [InlineData("cadastrar", "rejected", 400, "auth.registration_failed")]
    [InlineData("login", "rejected", 401, "auth.invalid_credentials")]
    [InlineData("refresh", "rejected", 401, "auth.invalid_credentials")]
    public async Task AuthEndpoint_ShouldReturnSafeProviderFailure(string route, string failure, int status, string code)
    {
        using var factory = new ApiFactory(failure);
        using var client = factory.CreateClient();
        object request = route switch
        {
            "cadastrar" => new CadastrarUsuarioRequest("Teste", "test@example.test", "TestPassword123",
                [new ConsentimentoRequest("TermosUso", "1.0"), new ConsentimentoRequest("Privacidade", "1.0")]),
            "login" => new LoginRequest("test@example.test", "TestPassword123"),
            "refresh" => new RefreshTokenRequest("fixture-opaque-token"),
            _ => throw new ArgumentOutOfRangeException(nameof(route))
        };

        using var response = await client.PostAsJsonAsync("/api/v1/auth/" + route, request);

        Assert.Equal(status, (int)response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal(code, error!.Code);
        Assert.False(string.IsNullOrWhiteSpace(error.TraceId));
        Assert.DoesNotContain("fixture-provider-detail", error.Message);
        Assert.DoesNotContain("TestPassword123", error.Message);
        Assert.DoesNotContain("fixture-opaque-token", error.Message);
        Assert.Equal(1, factory.Handler.Calls);
        using var scope = factory.Services.CreateScope();
        using var providerClient = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(IAuthenticationService));
        Assert.Equal(TimeSpan.FromSeconds(15), providerClient.Timeout);
        var db = scope.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>();
        Assert.Empty(await db.Usuarios.ToListAsync());
    }

    private sealed class ApiFactory(string failure) : WebApplicationFactory<Program>
    {
        private readonly string _database = Path.Combine(Path.GetTempPath(), $"equilibrafit-pp-auth-failure-{Guid.NewGuid():N}.db");
        internal FailureHandler Handler { get; } = new(failure);

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("SUPABASE_URL", "https://supabase.example.test");
            builder.UseSetting("SUPABASE_PUBLISHABLE_KEY", "public-test-placeholder");
            builder.UseSetting("SUPABASE_DB_CONNECTION_STRING", "");
            builder.UseSetting("DATABASE_PROVIDER", "Sqlite");
            builder.UseSetting("ConnectionStrings:Default", $"Data Source={_database};Pooling=False");
            builder.UseSetting("AdminBootstrap:Enabled", "false");
            builder.UseSetting("GooglePlay:Enabled", "false");
            builder.ConfigureTestServices(services =>
            {
                services.AddHttpClient<IAuthenticationService, SupabaseAuthenticationService>()
                    .ConfigurePrimaryHttpMessageHandler(() => Handler);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                Handler.Dispose();
                if (File.Exists(_database)) File.Delete(_database);
            }
        }
    }

    private sealed class FailureHandler(string failure) : HttpMessageHandler
    {
        internal int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            if (failure == "timeout") return Task.FromException<HttpResponseMessage>(new TaskCanceledException("fixture-provider-detail"));
            if (failure == "network") return Task.FromException<HttpResponseMessage>(new HttpRequestException("fixture-provider-detail"));
            var status = failure switch
            {
                "gateway" => HttpStatusCode.GatewayTimeout,
                "unavailable" => HttpStatusCode.ServiceUnavailable,
                "rejected" => HttpStatusCode.BadRequest,
                _ => HttpStatusCode.OK
            };
            return Task.FromResult(new HttpResponseMessage(status)
            { Content = new StringContent("fixture-provider-detail") });
        }
    }
}
