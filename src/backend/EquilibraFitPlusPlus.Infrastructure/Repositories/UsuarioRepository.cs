using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for user reads.
/// </summary>
public sealed class UsuarioRepository : IUsuarioRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the repository.
    /// </summary>
    public UsuarioRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<Usuario?> ObterAtivoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
    {
        return _dbContext.Usuarios
            .FirstOrDefaultAsync(usuario => usuario.TenantId == tenantId && usuario.Id == usuarioId && usuario.Status == UsuarioStatus.Ativo, cancellationToken);
    }
}
