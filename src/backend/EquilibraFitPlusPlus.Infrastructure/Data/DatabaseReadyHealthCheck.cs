using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EquilibraFitPlusPlus.Infrastructure.Data;

public sealed class DatabaseReadyHealthCheck(EquilibraFitPlusPlusDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await db.Tenants.AsNoTracking().AnyAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("Database seed unavailable.");
            if (db.Database.IsNpgsql() && (await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                return HealthCheckResult.Unhealthy("Database migrations pending.");
            return HealthCheckResult.Healthy();
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Database or schema unavailable.");
        }
    }
}
