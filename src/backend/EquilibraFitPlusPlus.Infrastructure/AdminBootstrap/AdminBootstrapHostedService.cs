using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EquilibraFitPlusPlus.Infrastructure.AdminBootstrap;

/// <summary>Promotes an already authenticated profile when explicitly configured by the operator.</summary>
public sealed class AdminBootstrapHostedService(
    IServiceScopeFactory scopes, IOptions<AdminBootstrapOptions> settings,
    ILogger<AdminBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        if (!settings.Value.Enabled) return;
        if (settings.Value.SupabaseUserId == Guid.Empty)
            throw new InvalidOperationException("Configure AdminBootstrap__SupabaseUserId for a verified existing profile.");
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>();
        Usuario user = await db.Usuarios.SingleOrDefaultAsync(x => x.IdentityUserId == settings.Value.SupabaseUserId, ct)
            ?? throw new InvalidOperationException("Administrator must confirm email and sign in before bootstrap.");
        if (user.Status != UsuarioStatus.Ativo) throw new InvalidOperationException("Administrator profile must be active.");
        if (user.Role != UsuarioRole.Administrador)
        {
            user.Role = UsuarioRole.Administrador;
            db.Auditorias.Add(new Auditoria
            {
                TenantId = user.TenantId, Acao = "admin.bootstrap",
                Entidade = "Usuario", EntidadeId = user.Id
            });
            await db.SaveChangesAsync(ct);
        }
        logger.LogInformation("Administrator bootstrap completed.");
    }
    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
