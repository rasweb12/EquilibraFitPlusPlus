using EquilibraFitPlusPlus.Application.Abstractions.Planos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for generated diet plans.
/// </summary>
public sealed class PlanoAlimentarRepository : IPlanoAlimentarRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the repository.
    /// </summary>
    public PlanoAlimentarRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<PerfilSaude?> ObterPerfilAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        return _dbContext.PerfisSaude
            .FirstOrDefaultAsync(perfil => perfil.TenantId == tenantId && perfil.UsuarioId == usuarioId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PlanoUsuario?> ObterPlanoAtivoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        return _dbContext.PlanosUsuario
            .Include(plano => plano.MetasNutricionais)
            .OrderByDescending(plano => plano.Versao)
            .ThenByDescending(plano => plano.CriadoEm)
            .FirstOrDefaultAsync(
                plano => plano.TenantId == tenantId && plano.UsuarioId == usuarioId && plano.Status == StatusPlano.Ativo,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> ObterProximaVersaoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        int ultimaVersao = await _dbContext.PlanosUsuario
            .Where(plano => plano.TenantId == tenantId && plano.UsuarioId == usuarioId)
            .Select(plano => (int?)plano.Versao)
            .MaxAsync(cancellationToken) ?? 0;

        return ultimaVersao + 1;
    }

    /// <inheritdoc />
    public void AdicionarPlano(PlanoUsuario plano)
    {
        _dbContext.PlanosUsuario.Add(plano);
    }
}
