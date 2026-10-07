using EquilibraFitPlusPlus.Application.Abstractions.Billing;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EquilibraFitPlusPlus.Infrastructure.Billing;

public sealed class GooglePlayReconciliationWorker(
    IServiceScopeFactory scopes, GooglePlayOptions options, ILogger<GooglePlayReconciliationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EquilibraFitPlusPlusDbContext>();
                var cutoff = DateTimeOffset.UtcNow.AddMinutes(-15);
                var receipts = await db.Assinaturas.AsNoTracking()
                    .Where(x => x.Plataforma == "GOOGLE_PLAY" && x.PurchaseTokenEncrypted != null && x.UltimoProcessamentoEm < cutoff)
                    .OrderBy(x => x.UltimoProcessamentoEm).Select(x => new { x.Id, x.PurchaseTokenEncrypted })
                    .Take(50).ToArrayAsync(stoppingToken);
                foreach (var receipt in receipts)
                {
                    try
                    {
                        using var itemScope = scopes.CreateScope();
                        var protector = itemScope.ServiceProvider.GetRequiredService<BillingTokenProtector>();
                        await itemScope.ServiceProvider.GetRequiredService<IBillingService>().ProcessNotificationAsync(
                            protector.Decrypt(receipt.PurchaseTokenEncrypted!), $"reconcile:{receipt.Id:N}:{Guid.NewGuid():N}", stoppingToken);
                    }
                    catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                    {
                        logger.LogWarning("Billing reconciliation failed for {SubscriptionId}: {ErrorType}", receipt.Id, ex.GetType().Name);
                    }
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Billing reconciliation batch failed: {ErrorType}", ex.GetType().Name);
            }
        }
    }
}
