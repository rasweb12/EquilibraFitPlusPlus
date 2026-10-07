using System.Net;
using System.Text;
using System.Text.Json;
using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Authentication;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using EquilibraFitPlusPlus.Shared.Results;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Authentication;

public sealed class SupabaseAuthenticationServiceTests
{
    [Fact]
    public async Task SignupWithoutConfirmation_ShouldNotProvisionAnUnverifiedProfile()
    {
        var subject = Guid.NewGuid();
        using var http = CreateHttp(new { user = new { id = subject, email = "test@example.test" } });
        await using var db = CreateDatabase();
        var service = CreateService(http, db);
        var result = await service.CadastrarAsync(new CadastrarUsuarioRequest(
            "Teste", "test@example.test", "TestPassword123",
            [new ConsentimentoRequest("Privacidade", "1.0")]), null, CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal("auth.email_confirmation_required", result.Errors.Single().Code);
        Assert.Empty(await db.Usuarios.ToListAsync());
    }

    [Fact]
    public async Task Login_ShouldUseProviderSessionAndIgnoreMetadataPermissions()
    {
        var subject = Guid.NewGuid();
        using var http = CreateHttp(new
        {
            access_token = "provider-access", refresh_token = "provider-refresh", expires_in = 3600,
            user = new { id = subject, email = "test@example.test",
                user_metadata = new { nome = "Teste", role = "Administrador", tenant_id = "forged" } }
        });
        await using var db = CreateDatabase();
        var service = CreateService(http, db);
        var result = await service.LoginAsync(new LoginRequest("test@example.test", "TestPassword123"), null, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("provider-access", result.Value!.AccessToken);
        Assert.Equal("provider-refresh", result.Value.RefreshToken);
        Assert.Equal(SeedData.DefaultTenantId, result.Value.Usuario.TenantId);
        Assert.Equal(UsuarioRole.Usuario.ToString(), result.Value.Usuario.Role);
        Assert.Equal(subject, (await db.Usuarios.SingleAsync()).IdentityUserId);
        Assert.Empty(await db.RefreshTokens.ToListAsync());
    }

    [Fact]
    public async Task Login_ShouldRejectDisabledProfileEvenWithValidProviderSession()
    {
        var subject = Guid.NewGuid();
        using var http = CreateHttp(new
        {
            access_token = "provider-access", refresh_token = "provider-refresh", expires_in = 3600,
            user = new { id = subject, email = "test@example.test" }
        });
        await using var db = CreateDatabase();
        db.Usuarios.Add(new Usuario
        {
            IdentityUserId = subject, Nome = "Teste", Email = "test@example.test",
            TenantId = SeedData.DefaultTenantId, ExcluidoEm = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        var service = CreateService(http, db);
        var result = await service.LoginAsync(new LoginRequest("test@example.test", "TestPassword123"), null, CancellationToken.None);
        Assert.True(result.IsFailure);
    }

    /// <summary>Provider timeouts must not create a profile or retry credential operations.</summary>
    [Theory]
    [InlineData("signup")]
    [InlineData("login")]
    [InlineData("refresh")]
    public async Task ProviderTimeout_ShouldReturnSafeFailure(string operation)
    {
        using var handler = new FailureHandler(_ => Task.FromException<HttpResponseMessage>(new TaskCanceledException("fixture-timeout")));
        using var http = CreateHttp(handler);
        await using var db = CreateDatabase();

        var result = await InvokeAsync(CreateService(http, db), operation, CancellationToken.None);

        Assert.Equal("auth.provider_timeout", result.Errors.Single().Code);
        Assert.Equal(1, handler.Calls);
        Assert.Empty(await db.Usuarios.ToListAsync());
    }

    /// <summary>DNS, TLS and transport failures remain safe temporary errors.</summary>
    [Theory]
    [InlineData("signup")]
    [InlineData("login")]
    [InlineData("refresh")]
    public async Task TransportFailure_ShouldNotBeReportedAsInvalidCredentials(string operation)
    {
        using var handler = new FailureHandler(_ => Task.FromException<HttpResponseMessage>(new HttpRequestException("fixture-network-error")));
        using var http = CreateHttp(handler);
        await using var db = CreateDatabase();

        var result = await InvokeAsync(CreateService(http, db), operation, CancellationToken.None);

        Assert.Equal("auth.provider_unavailable", result.Errors.Single().Code);
        Assert.DoesNotContain("fixture-network-error", result.Errors.Single().Message);
        Assert.Equal(1, handler.Calls);
        Assert.Empty(await db.Usuarios.ToListAsync());
    }

    /// <summary>Cancellation from the caller must propagate, not become a provider timeout.</summary>
    [Theory]
    [InlineData("signup")]
    [InlineData("login")]
    [InlineData("refresh")]
    public async Task CallerCancellation_ShouldPropagate(string operation)
    {
        using var source = new CancellationTokenSource();
        using var handler = new FailureHandler(ct =>
        {
            source.Cancel();
            return Task.FromCanceled<HttpResponseMessage>(ct);
        });
        using var http = CreateHttp(handler);
        await using var db = CreateDatabase();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => InvokeAsync(CreateService(http, db), operation, source.Token));

        Assert.Equal(1, handler.Calls);
        Assert.Empty(await db.Usuarios.ToListAsync());
    }

    /// <summary>HTTP failures from the provider use the same safe transport contract.</summary>
    [Theory]
    [InlineData(408, "auth.provider_timeout")]
    [InlineData(504, "auth.provider_timeout")]
    [InlineData(500, "auth.provider_unavailable")]
    [InlineData(502, "auth.provider_unavailable")]
    [InlineData(503, "auth.provider_unavailable")]
    public async Task ProviderHttpFailure_ShouldReturnTemporaryError(int status, string code)
    {
        using var handler = new FailureHandler(_ => Task.FromResult(new HttpResponseMessage((HttpStatusCode)status)
        { Content = new StringContent("fixture-provider-details") }));
        using var http = CreateHttp(handler);
        await using var db = CreateDatabase();

        var result = await InvokeAsync(CreateService(http, db), "signup", CancellationToken.None);

        Assert.Equal(code, result.Errors.Single().Code);
        Assert.DoesNotContain("fixture-provider-details", result.Errors.Single().Message);
        Assert.Equal(1, handler.Calls);
        Assert.Empty(await db.Usuarios.ToListAsync());
    }

    /// <summary>Exercise a real HttpClient timer without contacting Supabase.</summary>
    [Fact]
    public async Task HttpClientDeadline_ShouldBeHandledAsProviderTimeout()
    {
        using var handler = new FailureHandler(async ct =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var http = CreateHttp(handler);
        http.Timeout = TimeSpan.FromMilliseconds(100);
        await using var db = CreateDatabase();

        var result = await InvokeAsync(CreateService(http, db), "signup", CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal("auth.provider_timeout", result.Errors.Single().Code);
        Assert.Equal(1, handler.Calls);
        Assert.Empty(await db.Usuarios.ToListAsync());
    }

    /// <summary>Malformed provider responses must not provision users or expose the response.</summary>
    [Fact]
    public async Task InvalidJson_ShouldReturnSafeProviderFailure()
    {
        using var http = CreateHttp(new ResponseHandler("fixture-invalid-json"));
        await using var db = CreateDatabase();

        var result = await InvokeAsync(CreateService(http, db), "signup", CancellationToken.None);

        Assert.Equal("auth.provider_unavailable", result.Errors.Single().Code);
        Assert.Empty(await db.Usuarios.ToListAsync());
    }

    private static Task<Result<AuthResponse>> InvokeAsync(SupabaseAuthenticationService service, string operation, CancellationToken ct) => operation switch
    {
        "signup" => service.CadastrarAsync(new CadastrarUsuarioRequest("Teste", "test@example.test", "TestPassword123",
            [new ConsentimentoRequest("Privacidade", "1.0")]), null, ct),
        "login" => service.LoginAsync(new LoginRequest("test@example.test", "TestPassword123"), null, ct),
        "refresh" => service.RefreshAsync(new RefreshTokenRequest("fixture-opaque-token"), null, ct),
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };

    private static SupabaseAuthenticationService CreateService(HttpClient http, EquilibraFitPlusPlusDbContext db) =>
        new(http, db, NullLogger<SupabaseAuthenticationService>.Instance);

    private static EquilibraFitPlusPlusDbContext CreateDatabase() =>
        new(new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static HttpClient CreateHttp(object payload) =>
        CreateHttp(new ResponseHandler(JsonSerializer.Serialize(payload)));

    private static HttpClient CreateHttp(HttpMessageHandler handler) =>
        new(handler, disposeHandler: false) { BaseAddress = new Uri("https://supabase.example.test/auth/v1/") };

    private sealed class FailureHandler(Func<CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        internal int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return send(ct);
        }
    }

    private sealed class ResponseHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
