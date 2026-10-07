using EquilibraFitPlusPlus.Application.Abstractions.Onboarding;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for onboarding persistence.
/// </summary>
public sealed class OnboardingRepository : IOnboardingRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the repository.
    /// </summary>
    public OnboardingRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<PerfilSaude?> ObterPorUsuarioAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        return _dbContext.PerfisSaude
            .FirstOrDefaultAsync(perfil => perfil.TenantId == tenantId && perfil.UsuarioId == usuarioId, cancellationToken);
    }

    /// <inheritdoc />
    public void Adicionar(PerfilSaude perfilSaude)
    {
        _dbContext.PerfisSaude.Add(perfilSaude);
    }
}
