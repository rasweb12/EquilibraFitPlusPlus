using System.Net;
using System.Net.Http.Json;
using EquilibraFitPlusPlus.Admin.Services;
using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Shared.Pagination;
using Microsoft.JSInterop;

namespace EquilibraFitPlusPlus.Admin.UnitTests;

public sealed class AdminSessionTests
{
    [Fact]
    public async Task ExpiredAccess_ShouldRefreshOnceAndRetryWithRotatedToken()
    {
        var session = Session();
        var handler = new ApiHandler(session);
        using var api = new AdminApiClient(new Clients(handler), session);
        await api.ListSubscriptionsAsync(1, 20, default);
        Assert.Equal(1, handler.Refreshes);
        Assert.Equal("new-access", session.AccessToken);
        Assert.Equal("new-refresh", session.RefreshToken);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task LogoutOrAccountSwitchDuringRefresh_ShouldNotRevivePreviousSession(bool logout)
    {
        var session = Session();
        var handler = new ApiHandler(session) { BeforeRefreshResponse = () =>
        {
            if (logout) session.Clear();
            else session.Start(Auth(Guid.NewGuid(), "other-access", "other-refresh"));
        } };
        using var api = new AdminApiClient(new Clients(handler), session);
        await Assert.ThrowsAsync<AdminApiException>(() => api.ListSubscriptionsAsync(1, 20, default));
        Assert.Equal(logout ? null : "other-access", session.AccessToken);
    }

    [Fact]
    public async Task RejectedRefresh_ShouldClearExpiredSession()
    {
        var session = Session();
        using var api = new AdminApiClient(new Clients(new ApiHandler(session) { RejectRefresh = true }), session);
        await Assert.ThrowsAsync<AdminApiException>(() => api.ListSubscriptionsAsync(1, 20, default));
        Assert.False(session.IsAuthenticated);
    }

    [Fact]
    public async Task OfflineLogout_ShouldStillClearLocalSession()
    {
        var session = Session();
        using var api = new AdminApiClient(new Clients(new ApiHandler(session) { Offline = true }), session);
        await api.LogoutAsync(default);
        Assert.False(session.IsAuthenticated);
    }

    private static AuthResponse Auth(Guid user, string access, string refresh) => new(access, 3600, refresh,
        new UsuarioAutenticadoResponse(user, Guid.NewGuid(), "Admin", "admin@example.test", "Administrador"));
    private static AdminSessionState Session()
    {
        var session = new AdminSessionState(new Js());
        session.Start(Auth(Guid.NewGuid(), "old-access", "old-refresh"));
        return session;
    }
    private sealed class Js : IJSRuntime
    {
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => ValueTask.FromResult(default(T)!);
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken ct, object?[]? args) => ValueTask.FromResult(default(T)!);
    }
    private sealed class Clients(ApiHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, false) { BaseAddress = new Uri("https://api.example.test") };
    }
    private sealed class ApiHandler(AdminSessionState session) : HttpMessageHandler
    {
        public int Refreshes { get; private set; }
        public Action? BeforeRefreshResponse { get; init; }
        public bool RejectRefresh { get; init; }
        public bool Offline { get; init; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            if (Offline) throw new HttpRequestException("Offline test");
            if (request.RequestUri!.AbsolutePath.EndsWith("/refresh"))
            {
                Refreshes++;
                var user = session.User!.Id;
                BeforeRefreshResponse?.Invoke();
                return Task.FromResult(new HttpResponseMessage(RejectRefresh ? HttpStatusCode.Unauthorized : HttpStatusCode.OK)
                { Content = JsonContent.Create(Auth(user, "new-access", "new-refresh")) });
            }
            if (request.Headers.Authorization?.Parameter != "new-access")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = JsonContent.Create(new { }) });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            { Content = JsonContent.Create(new PagedResult<AssinaturaResumoResponse>([], 1, 20, 0)) });
        }
    }
}
