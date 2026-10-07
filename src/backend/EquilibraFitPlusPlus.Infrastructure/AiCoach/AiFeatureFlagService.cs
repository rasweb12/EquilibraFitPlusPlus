using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.AiCoach;

/// <summary>
/// Tenant-scoped AI feature flag reader.
/// </summary>
public sealed class AiFeatureFlagService : IAiFeatureFlagService
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the service.
    /// </summary>
    public AiFeatureFlagService(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(Guid tenantId, string key, bool defaultValue, CancellationToken cancellationToken)
    {
        FeatureFlag? flag = await _dbContext.FeatureFlags
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.TenantId == tenantId && item.Chave == key, cancellationToken);

        return flag?.Habilitada ?? defaultValue;
    }
}
