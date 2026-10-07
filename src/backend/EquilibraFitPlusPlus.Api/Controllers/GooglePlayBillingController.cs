using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Billing;
using EquilibraFitPlusPlus.Infrastructure.Billing;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

[Route("api/v1/billing/google-play")]
public sealed class GooglePlayBillingController(IBillingService billing, GooglePlayOptions options) : ApiControllerBase
{
    public sealed record PurchaseRequest([Required, MaxLength(4096)] string PurchaseToken);
    public sealed record PushMessage(string Data, string MessageId);
    public sealed record PushRequest(PushMessage Message);

    [Authorize]
    [HttpGet("products")]
    public IActionResult Products() => Ok(new { enabled = options.Enabled, productIds = options.ProductIds });

    [Authorize]
    [HttpPost("verify")]
    public async Task<IActionResult> Verify(PurchaseRequest request, CancellationToken ct)
    {
        if (!TryGetUserContext(out var tenantId, out var usuarioId)) return UnauthorizedUserContext();
        return Ok(await billing.ProcessAsync(tenantId, usuarioId, request.PurchaseToken, $"client:{Guid.NewGuid():N}", ct));
    }

    [AllowAnonymous]
    [RequestSizeLimit(65536)]
    [HttpPost("notifications")]
    public async Task<IActionResult> Notification(PushRequest push, CancellationToken ct)
    {
        if (!options.Enabled || string.IsNullOrEmpty(options.PubSubAudience) || string.IsNullOrEmpty(options.PubSubServiceAccountEmail))
            return StatusCode(503);
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.Ordinal)) return Unauthorized();
        try
        {
            var identity = await GoogleJsonWebSignature.ValidateAsync(authorization[7..],
                new GoogleJsonWebSignature.ValidationSettings { Audience = [options.PubSubAudience] });
            if (!identity.EmailVerified || identity.Email != options.PubSubServiceAccountEmail) return Unauthorized();
        }
        catch (InvalidJwtException) { return Unauthorized(); }
        if (push.Message == null || string.IsNullOrWhiteSpace(push.Message.MessageId) || push.Message.MessageId.Length > 128)
            return BadRequest();
        try
        {
            using var json = JsonDocument.Parse(Convert.FromBase64String(push.Message.Data));
            if (json.RootElement.GetProperty("packageName").GetString() != options.PackageName) return BadRequest();
            if (json.RootElement.TryGetProperty("subscriptionNotification", out var notification))
            {
                var token = notification.GetProperty("purchaseToken").GetString();
                if (string.IsNullOrEmpty(token)) return BadRequest();
                await billing.ProcessNotificationAsync(token, $"rtdn:{push.Message.MessageId}", ct);
            }
            return NoContent();
        }
        catch (Exception ex) when (ex is FormatException or JsonException or KeyNotFoundException or InvalidOperationException)
        { return BadRequest(); }
    }
}
