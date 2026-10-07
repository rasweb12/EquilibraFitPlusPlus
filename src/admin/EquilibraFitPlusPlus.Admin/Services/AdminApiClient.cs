using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Contracts.Common;
using EquilibraFitPlusPlus.Contracts.Gamificacao;
using EquilibraFitPlusPlus.Contracts.Lgpd;
using EquilibraFitPlusPlus.Contracts.Marketplace;
using EquilibraFitPlusPlus.Contracts.Notificacoes;
using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Contracts.Relatorios;
using EquilibraFitPlusPlus.Shared.Pagination;

namespace EquilibraFitPlusPlus.Admin.Services;

/// <summary>
/// Typed HTTP client used by the administrative web application.
/// </summary>
public sealed class AdminApiClient : IDisposable
{
    /// <summary>Named HttpClient registration.</summary>
    public const string HttpClientName = "EquilibraFitPlusPlus.Admin.Api";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AdminSessionState _session;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    /// <summary>Initializes the client.</summary>
    public AdminApiClient(IHttpClientFactory httpClientFactory, AdminSessionState session)
    {
        _httpClientFactory = httpClientFactory;
        _session = session;
    }

    /// <summary>Authenticates an administrator using the backend auth endpoint.</summary>
    public async Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => CreateClient(includeAuthorization: false).PostAsJsonAsync(
                "/api/v1/auth/login",
                new LoginRequest(email.Trim(), password),
                JsonOptions,
                cancellationToken),
            cancellationToken, allowRefresh: false);

        AuthResponse auth = await ReadAsync<AuthResponse>(response, cancellationToken);
        if (auth.Usuario.Role is not ("Administrador" or "SuperAdmin"))
        {
            throw new AdminApiException("Este usuario nao possui acesso administrativo.");
        }

        _session.Start(auth);
        await _session.StoreAsync(cancellationToken);
        return auth;
    }

    /// <summary>Gets the administrative dashboard.</summary>
    public async Task LogoutAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var client = CreateClient();
            using var response = await client.PostAsync("/api/v1/auth/logout", null, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Local logout must remain available while the API is offline.
        }
        finally { await _session.ClearAsync(CancellationToken.None); }
    }

    /// <summary>Gets the administrative dashboard.</summary>
    public Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken)
    {
        return GetAsync<AdminDashboardResponse>("/api/v1/admin/dashboard", cancellationToken);
    }

    /// <summary>Gets AI operation status, metrics, history and teaching configuration.</summary>
    public Task<AdminIaOperacaoResponse> GetIaOperationAsync(CancellationToken cancellationToken)
    {
        return GetAsync<AdminIaOperacaoResponse>("/api/v1/admin/ia/operacao", cancellationToken);
    }

    /// <summary>Lists AI teaching versions.</summary>
    public Task<IReadOnlyCollection<AdminIaEnsinoVersaoResponse>> ListIaTeachingVersionsAsync(CancellationToken cancellationToken)
    {
        return GetAsync<IReadOnlyCollection<AdminIaEnsinoVersaoResponse>>("/api/v1/admin/ia/ensino/versoes", cancellationToken);
    }

    /// <summary>Tests an AI teaching draft.</summary>
    public async Task<TestarAdminIaEnsinoResponse> TestIaTeachingAsync(TestarAdminIaEnsinoRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => CreateClient().PostAsJsonAsync("/api/v1/admin/ia/ensino/testar", request, JsonOptions, cancellationToken),
            cancellationToken);
        return await ReadAsync<TestarAdminIaEnsinoResponse>(response, cancellationToken);
    }

    /// <summary>Publishes AI teaching configuration.</summary>
    public async Task<AdminIaEnsinoResponse> PublishIaTeachingAsync(PublicarAdminIaEnsinoRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => CreateClient().PutAsJsonAsync("/api/v1/admin/ia/ensino/publicar", request, JsonOptions, cancellationToken),
            cancellationToken);
        return await ReadAsync<AdminIaEnsinoResponse>(response, cancellationToken);
    }

    /// <summary>Restores an AI teaching version.</summary>
    public async Task<AdminIaEnsinoResponse> RestoreIaTeachingAsync(RestaurarAdminIaEnsinoRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => CreateClient().PostAsJsonAsync("/api/v1/admin/ia/ensino/restaurar", request, JsonOptions, cancellationToken),
            cancellationToken);
        return await ReadAsync<AdminIaEnsinoResponse>(response, cancellationToken);
    }

    /// <summary>Lists users with support-safe fields.</summary>
    public Task<PagedResult<AdminUsuarioResumoResponse>> ListUsersAsync(string? term, int page, int pageSize, CancellationToken cancellationToken)
    {
        string query = $"/api/v1/admin/usuarios?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(term))
        {
            query += $"&termo={Uri.EscapeDataString(term.Trim())}";
        }

        return GetAsync<PagedResult<AdminUsuarioResumoResponse>>(query, cancellationToken);
    }

    /// <summary>Revokes active sessions for a user.</summary>
    public async Task<RevogarSessoesUsuarioResponse> RevokeUserSessionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => CreateClient().PostAsync($"/api/v1/admin/usuarios/{userId}/revogar-sessoes", content: null, cancellationToken),
            cancellationToken);
        return await ReadAsync<RevogarSessoesUsuarioResponse>(response, cancellationToken);
    }

    /// <summary>Lists audit entries.</summary>
    public Task<PagedResult<AuditoriaResponse>> ListAuditAsync(string? entity, int page, int pageSize, CancellationToken cancellationToken)
    {
        string query = $"/api/v1/admin/auditorias?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(entity))
        {
            query += $"&entidade={Uri.EscapeDataString(entity.Trim())}";
        }

        return GetAsync<PagedResult<AuditoriaResponse>>(query, cancellationToken);
    }

    /// <summary>Lists feature flags.</summary>
    public Task<PagedResult<FeatureFlagResponse>> ListFeatureFlagsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return GetAsync<PagedResult<FeatureFlagResponse>>($"/api/v1/admin/feature-flags?page={page}&pageSize={pageSize}", cancellationToken);
    }

    /// <summary>Saves a feature flag and returns the persisted state.</summary>
    public async Task<FeatureFlagResponse> SaveFeatureFlagAsync(SalvarFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => CreateClient().PutAsJsonAsync("/api/v1/admin/feature-flags", request, JsonOptions, cancellationToken),
            cancellationToken);
        return await ReadAsync<FeatureFlagResponse>(response, cancellationToken);
    }

    /// <summary>Lists subscriptions.</summary>
    public Task<PagedResult<AssinaturaResumoResponse>> ListSubscriptionsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return GetAsync<PagedResult<AssinaturaResumoResponse>>($"/api/v1/admin/premium/assinaturas?page={page}&pageSize={pageSize}", cancellationToken);
    }

    /// <summary>Lists coupons.</summary>
    public Task<PagedResult<CupomResponse>> ListCouponsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return GetAsync<PagedResult<CupomResponse>>($"/api/v1/admin/premium/cupons?page={page}&pageSize={pageSize}", cancellationToken);
    }

    /// <summary>Saves a coupon.</summary>
    public async Task<CupomResponse> SaveCouponAsync(SalvarCupomRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => CreateClient().PutAsJsonAsync("/api/v1/admin/premium/cupons", request, JsonOptions, cancellationToken),
            cancellationToken);
        return await ReadAsync<CupomResponse>(response, cancellationToken);
    }

    /// <summary>Lists marketplace partners.</summary>
    public Task<PagedResult<ParceiroResponse>> ListPartnersAsync(string? term, int page, int pageSize, CancellationToken cancellationToken)
    {
        string query = $"/api/v1/admin/marketplace/parceiros?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(term))
        {
            query += $"&termo={Uri.EscapeDataString(term.Trim())}";
        }

        return GetAsync<PagedResult<ParceiroResponse>>(query, cancellationToken);
    }

    /// <summary>Saves a marketplace partner.</summary>
    public async Task<ParceiroResponse> SavePartnerAsync(SalvarParceiroRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => CreateClient().PutAsJsonAsync("/api/v1/admin/marketplace/parceiros", request, JsonOptions, cancellationToken),
            cancellationToken);
        return await ReadAsync<ParceiroResponse>(response, cancellationToken);
    }

    /// <summary>Lists notifications.</summary>
    public Task<PagedResult<NotificacaoResponse>> ListNotificationsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        return GetAsync<PagedResult<NotificacaoResponse>>($"/api/v1/admin/notificacoes?page={page}&pageSize={pageSize}", cancellationToken);
    }

    /// <summary>Creates a notification.</summary>
    public async Task<NotificacaoResponse> CreateNotificationAsync(CriarNotificacaoRequest request, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(
            () => CreateClient().PostAsJsonAsync("/api/v1/admin/notificacoes", request, JsonOptions, cancellationToken),
            cancellationToken);
        return await ReadAsync<NotificacaoResponse>(response, cancellationToken);
    }

    /// <summary>Gets administrative report summary.</summary>
    public Task<RelatorioAdminResumoResponse> GetAdminReportAsync(DateOnly? start, DateOnly? end, CancellationToken cancellationToken)
    {
        string query = "/api/v1/admin/relatorios/resumo";
        if (start.HasValue || end.HasValue)
        {
            List<string> parameters = [];
            if (start.HasValue)
            {
                parameters.Add($"inicio={start.Value:yyyy-MM-dd}");
            }

            if (end.HasValue)
            {
                parameters.Add($"fim={end.Value:yyyy-MM-dd}");
            }

            query += "?" + string.Join("&", parameters);
        }

        return GetAsync<RelatorioAdminResumoResponse>(query, cancellationToken);
    }

    /// <summary>Gets administrative gamification summary.</summary>
    public Task<GamificacaoAdminResumoResponse> GetAdminGamificationAsync(CancellationToken cancellationToken)
    {
        return GetAsync<GamificacaoAdminResumoResponse>("/api/v1/admin/gamificacao/resumo", cancellationToken);
    }

    /// <summary>Gets administrative LGPD summary.</summary>
    public Task<LgpdAdminResumoResponse> GetAdminLgpdAsync(CancellationToken cancellationToken)
    {
        return GetAsync<LgpdAdminResumoResponse>("/api/v1/admin/lgpd/resumo", cancellationToken);
    }

    private async Task<T> GetAsync<T>(string requestUri, CancellationToken cancellationToken)
    {
        using var response = await SendAsync(() => CreateClient().GetAsync(requestUri, cancellationToken), cancellationToken);
        return await ReadAsync<T>(response, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendAsync(
        Func<Task<HttpResponseMessage>> request,
        CancellationToken cancellationToken,
        bool allowRefresh = true)
    {
        try
        {
            var accessToken = _session.AccessToken;
            var response = await request();
            if (allowRefresh && response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
                !string.IsNullOrWhiteSpace(accessToken))
            {
                if (await RefreshAsync(accessToken, cancellationToken))
                {
                    response.Dispose();
                    response = await request();
                }
            }
            return response;
        }
        catch (HttpRequestException exception)
        {
            throw BuildConnectionException(exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw BuildConnectionException(exception);
        }
    }

    private async Task<bool> RefreshAsync(string previousAccessToken, CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            if (!_session.IsAuthenticated) return false;
            if (_session.AccessToken != previousAccessToken) return true;
            var previousRefreshToken = _session.RefreshToken;
            var previousUser = _session.User!.Id;
            if (string.IsNullOrWhiteSpace(previousRefreshToken)) return false;
            using var client = CreateClient(includeAuthorization: false);
            using var response = await client.PostAsJsonAsync("/api/v1/auth/refresh",
                new RefreshTokenRequest(previousRefreshToken), JsonOptions, ct);
            // A logout or account switch during refresh must not revive the old session.
            if (_session.RefreshToken != previousRefreshToken || _session.User?.Id != previousUser) return false;
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.BadRequest)
                    await _session.ClearAsync(ct);
                return false;
            }
            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions, ct);
            if (auth is null || auth.Usuario.Id != previousUser ||
                auth.Usuario.Role is not ("Administrador" or "SuperAdmin"))
            {
                await _session.ClearAsync(ct);
                return false;
            }
            _session.Start(auth);
            await _session.StoreAsync(ct);
            return true;
        }
        finally { _refreshLock.Release(); }
    }

    /// <summary>Releases the circuit-scoped refresh synchronization.</summary>
    public void Dispose() => _refreshLock.Dispose();

    private static AdminApiException BuildConnectionException(Exception exception)
    {
        return new AdminApiException(
            "Não conseguimos conectar com a API agora. Confira a configuração e tente novamente.",
            "api_unavailable",
            exception);
    }

    private HttpClient CreateClient(bool includeAuthorization = true)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        if (includeAuthorization && !string.IsNullOrWhiteSpace(_session.AccessToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        }

        return client;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            T? value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return value ?? throw new AdminApiException("A API retornou uma resposta vazia.");
        }

        ApiErrorResponse? error = null;
        try
        {
            error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(JsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            // The API can return infrastructure errors without the standard contract.
        }

        string? message = error?.Message;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = response.StatusCode == System.Net.HttpStatusCode.Unauthorized
                ? "Sessao administrativa expirada. Entre novamente para continuar."
                : "Nao conseguimos concluir a operacao administrativa agora.";
        }

        throw new AdminApiException(message, error?.Code);
    }
}
