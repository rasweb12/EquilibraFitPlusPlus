using System.Security.Claims;
using System.Text;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Api.Middleware;

/// <summary>
/// Replays successful responses for authenticated idempotent write retries.
/// </summary>
public sealed class IdempotencyMiddleware
{
    private const int MaxKeyLength = 120;
    private const int MaxStoredBodyLength = 20_000;
    private static readonly HashSet<string> SupportedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    private readonly RequestDelegate _next;

    /// <summary>
    /// Initializes the middleware.
    /// </summary>
    public IdempotencyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Handles the current request.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, EquilibraFitPlusPlusDbContext dbContext)
    {
        string? idempotencyKey = ReadIdempotencyKey(context);
        if (idempotencyKey is null ||
            !TryReadOwner(context, out Guid tenantId, out Guid usuarioId))
        {
            await _next(context);
            return;
        }

        string method = context.Request.Method.ToUpperInvariant();
        string path = context.Request.Path.Value ?? "/";
        DateTimeOffset now = DateTimeOffset.UtcNow;

        IdempotencyRecord? existing = await dbContext.IdempotencyRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(
                record =>
                    record.TenantId == tenantId &&
                    record.UsuarioId == usuarioId &&
                    record.Chave == idempotencyKey &&
                    record.Metodo == method &&
                    record.Caminho == path &&
                    record.ExpiresAt > now,
                context.RequestAborted);

        if (existing is not null)
        {
            context.Response.StatusCode = existing.ResponseStatusCode;
            if (!string.IsNullOrWhiteSpace(existing.ResponseContentType))
            {
                context.Response.ContentType = existing.ResponseContentType;
            }

            if (!string.IsNullOrEmpty(existing.ResponseBody))
            {
                await context.Response.WriteAsync(existing.ResponseBody, context.RequestAborted);
            }

            return;
        }

        Stream originalBody = context.Response.Body;
        await using var responseBuffer = new MemoryStream();
        context.Response.Body = responseBuffer;

        try
        {
            await _next(context);
            await StoreResponseIfSuccessfulAsync(
                context,
                dbContext,
                responseBuffer,
                tenantId,
                usuarioId,
                idempotencyKey,
                method,
                path);
        }
        finally
        {
            responseBuffer.Position = 0;
            await responseBuffer.CopyToAsync(originalBody, context.RequestAborted);
            context.Response.Body = originalBody;
        }
    }

    private static string? ReadIdempotencyKey(HttpContext context)
    {
        if (!SupportedMethods.Contains(context.Request.Method))
        {
            return null;
        }

        string? value = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length <= MaxKeyLength ? trimmed : null;
    }

    private static bool TryReadOwner(HttpContext context, out Guid tenantId, out Guid usuarioId)
    {
        tenantId = Guid.Empty;
        usuarioId = Guid.Empty;

        ClaimsPrincipal user = context.User;
        string? tenant = user.FindFirstValue("tenant_id");
        string? usuario = user.FindFirstValue("usuario_id");

        return Guid.TryParse(tenant, out tenantId) && Guid.TryParse(usuario, out usuarioId);
    }

    private static async Task StoreResponseIfSuccessfulAsync(
        HttpContext context,
        EquilibraFitPlusPlusDbContext dbContext,
        MemoryStream responseBuffer,
        Guid tenantId,
        Guid usuarioId,
        string idempotencyKey,
        string method,
        string path)
    {
        int statusCode = context.Response.StatusCode;
        if (statusCode is < 200 or >= 300)
        {
            return;
        }

        responseBuffer.Position = 0;
        using var reader = new StreamReader(responseBuffer, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        responseBuffer.Position = 0;

        if (body.Length > MaxStoredBodyLength)
        {
            body = string.Empty;
        }

        dbContext.IdempotencyRecords.Add(new IdempotencyRecord
        {
            TenantId = tenantId,
            UsuarioId = usuarioId,
            Chave = idempotencyKey,
            Metodo = method,
            Caminho = path,
            ResponseStatusCode = statusCode,
            ResponseContentType = context.Response.ContentType,
            ResponseBody = body,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(24)
        });

        try
        {
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
        }
    }
}
