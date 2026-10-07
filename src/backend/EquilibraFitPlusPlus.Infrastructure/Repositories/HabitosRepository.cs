using EquilibraFitPlusPlus.Application.Abstractions.Habitos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for user daily habits.
/// </summary>
public sealed class HabitosRepository : IHabitosRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the repository.
    /// </summary>
    public HabitosRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<RegistroHabitos?> ObterPorDataAsync(Guid tenantId, Guid usuarioId, DateOnly data, CancellationToken cancellationToken)
    {
        return _dbContext.RegistrosHabitos
            .FirstOrDefaultAsync(
                registro => registro.TenantId == tenantId && registro.UsuarioId == usuarioId && registro.Data == data,
                cancellationToken);
    }

    /// <inheritdoc />
    public void Adicionar(RegistroHabitos registro)
    {
        _dbContext.RegistrosHabitos.Add(registro);
    }
}
