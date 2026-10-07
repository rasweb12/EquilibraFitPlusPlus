using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Authentication;
using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Infrastructure.Data.Seed;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EquilibraFitPlusPlus.Infrastructure.Authentication;

/// <summary>Compatibility adapter for API clients; Supabase owns passwords and token rotation.</summary>
public sealed class SupabaseAuthenticationService(
    HttpClient http, EquilibraFitPlusPlusDbContext db,
    ILogger<SupabaseAuthenticationService> logger) : IAuthenticationService
{
    public async Task<Result<AuthResponse>> CadastrarAsync(CadastrarUsuarioRequest request, string? ipAddress, CancellationToken ct)
    {
        foreach (var consent in request.Aceites)
        {
            if (!Enum.TryParse<TipoConsentimento>(consent.Tipo, true, out var type) || !Enum.IsDefined(type))
                return Result<AuthResponse>.Failure(new Error("auth.invalid_consent", "Consentimento invalido."));
        }
        var result = await SendAuthRequestAsync("signup", new
        {
            email = request.Email.Trim(), password = request.Senha,
            data = new { nome = request.Nome.Trim(), aceites = request.Aceites }
        }, new Error("auth.registration_failed", "Nao foi possivel cadastrar. Verifique os dados e tente novamente."), ct);
        if (result.IsFailure) return Result<AuthResponse>.Failure(result.Errors);
        using var json = result.Value!;
        var root = json.RootElement;
        // An anonymous signup response does not prove ownership. Provision only with a session.
        if (!root.TryGetProperty("access_token", out var token) || token.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(token.GetString()))
            return Result<AuthResponse>.Failure(new Error("auth.email_confirmation_required", "Cadastro recebido. Confirme seu email e entre novamente."));
        return await CreateResponseAsync(root, ct);
    }

    public Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken ct) =>
        RequestTokenAsync("token?grant_type=password", new { email = request.Email.Trim(), password = request.Senha }, ct);

    public Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, string? ipAddress, CancellationToken ct) =>
        RequestTokenAsync("token?grant_type=refresh_token", new { refresh_token = request.RefreshToken }, ct);

    private async Task<Result<AuthResponse>> RequestTokenAsync(string path, object body, CancellationToken ct)
    {
        var result = await SendAuthRequestAsync(path, body,
            new Error("auth.invalid_credentials", "Sessao invalida. Verifique seu email e entre novamente."), ct);
        if (result.IsFailure) return Result<AuthResponse>.Failure(result.Errors);
        using var json = result.Value!;
        return await CreateResponseAsync(json.RootElement, ct);
    }

    private async Task<Result<JsonDocument>> SendAuthRequestAsync(string path, object body, Error rejection, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(path, body, ct);
            if (response.StatusCode is System.Net.HttpStatusCode.RequestTimeout or System.Net.HttpStatusCode.GatewayTimeout)
                return ProviderFailure(path, "auth.provider_timeout", (int)response.StatusCode);
            if ((int)response.StatusCode >= 500)
                return ProviderFailure(path, "auth.provider_unavailable", (int)response.StatusCode);
            if (!response.IsSuccessStatusCode) return Result<JsonDocument>.Failure(rejection);

            return Result<JsonDocument>.Success(await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ProviderFailure(path, "auth.provider_timeout");
        }
        catch (HttpRequestException) when (!ct.IsCancellationRequested)
        {
            return ProviderFailure(path, "auth.provider_unavailable");
        }
        catch (JsonException)
        {
            return ProviderFailure(path, "auth.provider_unavailable");
        }
    }

    private Result<JsonDocument> ProviderFailure(string path, string code, int? status = null)
    {
        logger.LogWarning("Supabase Auth request {Operation} failed with {Code} and provider status {StatusCode}.", path, code, status);
        string message = code == "auth.provider_timeout"
            ? "O servico de autenticacao demorou para responder. Tente novamente em instantes."
            : "O servico de autenticacao esta temporariamente indisponivel. Tente novamente em instantes.";
        return Result<JsonDocument>.Failure(new Error(code, message));
    }

    private async Task<Result<AuthResponse>> CreateResponseAsync(JsonElement root, CancellationToken ct)
    {
        var authUser = root.GetProperty("user");
        var subject = Guid.Parse(authUser.GetProperty("id").GetString()!);
        var user = await db.Usuarios.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.IdentityUserId == subject, ct);
        if (user is null)
        {
            string name = "Usuario";
            if (authUser.TryGetProperty("user_metadata", out var metadata) && metadata.TryGetProperty("nome", out var nameValue))
                name = nameValue.GetString() ?? name;
            user = new Usuario
            {
                Id = subject, IdentityUserId = subject, TenantId = SeedData.DefaultTenantId,
                Nome = name, Email = authUser.GetProperty("email").GetString()!
            };
            db.Usuarios.Add(user);
            if (authUser.TryGetProperty("user_metadata", out var userMetadata) &&
                userMetadata.TryGetProperty("aceites", out var consents) && consents.ValueKind == JsonValueKind.Array)
            {
                var seen = new HashSet<(TipoConsentimento, string)>();
                foreach (var consent in consents.EnumerateArray())
                {
                    if (!consent.TryGetProperty("tipo", out var typeValue) ||
                        !Enum.TryParse<TipoConsentimento>(typeValue.GetString(), true, out var type) ||
                        !consent.TryGetProperty("versao", out var versionValue)) continue;
                    var version = versionValue.GetString() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(version) || version.Length > 40 || !seen.Add((type, version))) continue;
                    db.ConsentimentosUsuario.Add(new ConsentimentoUsuario
                    {
                        TenantId = user.TenantId, UsuarioId = user.Id, Tipo = type, Versao = version, Origem = "SupabaseAuth"
                    });
                }
            }
        }
        if (user.ExcluidoEm is not null || user.Status != UsuarioStatus.Ativo)
            return Result<AuthResponse>.Failure(new Error("auth.invalid_credentials", "Conta indisponivel."));
        if (user.SessoesRevogadasAntesDe is { } cutoff)
        {
            var reader = new JwtSecurityTokenHandler();
            var access = root.GetProperty("access_token").GetString()!;
            if (!reader.CanReadToken(access) || SupabaseSessionPolicy.IsRevoked(reader.ReadJwtToken(access).Claims, cutoff))
                return Result<AuthResponse>.Failure(new Error("auth.session_revoked", "Entre novamente para iniciar uma nova sessao."));
        }
        user.UltimoLoginEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Result<AuthResponse>.Success(new AuthResponse(
            root.GetProperty("access_token").GetString()!, root.GetProperty("expires_in").GetInt32(),
            root.GetProperty("refresh_token").GetString()!,
            new UsuarioAutenticadoResponse(user.Id, user.TenantId, user.Nome, user.Email, user.Role.ToString())));
    }
}
