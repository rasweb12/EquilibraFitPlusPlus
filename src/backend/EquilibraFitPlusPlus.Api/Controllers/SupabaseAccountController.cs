using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using EquilibraFitPlusPlus.Infrastructure.Authentication;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Api.Controllers;

[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public sealed class SupabaseAccountController(
    EquilibraFitPlusPlusDbContext db, IHttpClientFactory clients, SupabaseOptions supabase) : ApiControllerBase
{
    public sealed record RecoveryRequest([Required, EmailAddress] string Email);
    public sealed record PasswordRequest([Required, MinLength(8), MaxLength(128)] string Senha);
    public sealed record ResetRequest([Required, MaxLength(512)] string TokenHash,
        [Required, MinLength(8), MaxLength(128)] string Senha);

    [AllowAnonymous]
    [HttpPost("recuperar")]
    public async Task<IActionResult> Recover(RecoveryRequest request, CancellationToken ct)
    {
        using var message = Message(HttpMethod.Post, "recover", new { email = request.Email.Trim() });
        using var response = await clients.CreateClient().SendAsync(message, ct);
        return Accepted(new { message = "Se o email estiver cadastrado, recebera as instrucoes de recuperacao." });
    }

    [Authorize]
    [HttpPut("senha")]
    public async Task<IActionResult> ChangePassword(PasswordRequest request, CancellationToken ct)
    {
        using var message = Message(HttpMethod.Put, "user", new { password = request.Senha }, authenticated: true);
        using var response = await clients.CreateClient().SendAsync(message, ct);
        return response.IsSuccessStatusCode ? NoContent() : BadRequest(new { code = "auth.password_update_failed" });
    }

    [AllowAnonymous]
    [HttpPost("recuperar/confirmar")]
    public async Task<IActionResult> ResetPassword(ResetRequest request, CancellationToken ct)
    {
        using var verify = Message(HttpMethod.Post, "verify", new { token_hash = request.TokenHash, type = "recovery" });
        using var verified = await clients.CreateClient().SendAsync(verify, ct);
        if (!verified.IsSuccessStatusCode) return BadRequest(new { code = "auth.recovery_invalid", message = "Link invalido ou expirado. Solicite outro." });
        using var document = await JsonDocument.ParseAsync(await verified.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var root = document.RootElement;
        if (!root.TryGetProperty("access_token", out var access) || string.IsNullOrWhiteSpace(access.GetString()) ||
            !root.TryGetProperty("user", out var authUser) || !authUser.TryGetProperty("id", out var id) ||
            !Guid.TryParse(id.GetString(), out var subject))
            return BadRequest(new { code = "auth.recovery_invalid" });
        // Verification proves ownership; revoke local sessions before changing the provider password.
        var profile = await db.Usuarios.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.IdentityUserId == subject, ct);
        if (profile != null)
        {
            profile.SessoesRevogadasAntesDe = DateTimeOffset.UtcNow;
            var sessions = await db.AuthSessions.Where(x => x.UsuarioId == profile.Id && x.RevogadoEm == null).ToArrayAsync(ct);
            foreach (var session in sessions) session.RevogadoEm = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        using var update = Message(HttpMethod.Put, "user", new { password = request.Senha });
        update.Headers.Authorization = new AuthenticationHeaderValue("Bearer", access.GetString());
        using var updated = await clients.CreateClient().SendAsync(update, ct);
        if (!updated.IsSuccessStatusCode) return BadRequest(new { code = "auth.password_update_failed" });
        using var logout = Message(HttpMethod.Post, "logout?scope=global", null);
        logout.Headers.Authorization = update.Headers.Authorization;
        using var loggedOut = await clients.CreateClient().SendAsync(logout, ct);
        return NoContent();
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!TryGetUserContext(out var tenant, out var user) ||
            !Guid.TryParse(User.FindFirstValue("session_id"), out var sessionId))
            return UnauthorizedUserContext();
        var session = await db.AuthSessions.SingleAsync(x =>
            x.SessionId == sessionId && x.UsuarioId == user && x.TenantId == tenant, ct);
        session.RevogadoEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        using var message = Message(HttpMethod.Post, "logout?scope=local", null, authenticated: true);
        using var response = await clients.CreateClient().SendAsync(message, ct);
        return NoContent();
    }

    private HttpRequestMessage Message(HttpMethod method, string path, object? body, bool authenticated = false)
    {
        var message = new HttpRequestMessage(method, new Uri(supabase.Url, "auth/v1/" + path));
        message.Headers.Add("apikey", supabase.PublishableKey);
        if (authenticated && AuthenticationHeaderValue.TryParse(Request.Headers.Authorization.ToString(), out var authorization))
            message.Headers.Authorization = authorization;
        if (body is not null) message.Content = JsonContent.Create(body);
        return message;
    }
}
