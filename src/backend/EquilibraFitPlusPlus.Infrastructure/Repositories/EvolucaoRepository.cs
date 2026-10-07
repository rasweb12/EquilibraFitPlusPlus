using EquilibraFitPlusPlus.Application.Abstractions.Evolucao;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for user body evolution logs.
/// </summary>
public sealed class EvolucaoRepository : IEvolucaoRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the repository.
    /// </summary>
    public EvolucaoRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<RegistroEvolucao?> ObterPorDataAsync(Guid tenantId, Guid usuarioId, DateOnly data, CancellationToken cancellationToken)
    {
        return _dbContext.RegistrosEvolucao
            .FirstOrDefaultAsync(
                registro => registro.TenantId == tenantId && registro.UsuarioId == usuarioId && registro.Data == data,
                cancellationToken);
    }

    /// <inheritdoc />
    public void Adicionar(RegistroEvolucao registro)
    {
        _dbContext.RegistrosEvolucao.Add(registro);
    }
}
