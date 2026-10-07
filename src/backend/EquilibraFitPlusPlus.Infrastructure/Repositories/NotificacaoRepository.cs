using EquilibraFitPlusPlus.Application.Abstractions.Notificacoes;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for notifications.
/// </summary>
public sealed class NotificacaoRepository : INotificacaoRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>Initializes the repository.</summary>
    public NotificacaoRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public Task<PagedResult<Notificacao>> ListarUsuarioAsync(Guid tenantId, Guid usuarioId, int page, int pageSize, CancellationToken cancellationToken)
    {
        return PageAsync(
            _dbContext.Notificacoes
                .Where(notificacao => notificacao.TenantId == tenantId && notificacao.UsuarioId == usuarioId)
                .OrderByDescending(notificacao => notificacao.CriadoEm),
            page,
            pageSize,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedResult<Notificacao>> ListarAdminAsync(Guid tenantId, int page, int pageSize, CancellationToken cancellationToken)
    {
        return PageAsync(
            _dbContext.Notificacoes
                .Where(notificacao => notificacao.TenantId == tenantId)
                .OrderByDescending(notificacao => notificacao.CriadoEm),
            page,
            pageSize,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<Notificacao?> ObterUsuarioNotificacaoAsync(Guid tenantId, Guid usuarioId, Guid notificacaoId, CancellationToken cancellationToken)
    {
        return _dbContext.Notificacoes.FirstOrDefaultAsync(
            notificacao => notificacao.TenantId == tenantId && notificacao.UsuarioId == usuarioId && notificacao.Id == notificacaoId,
            cancellationToken);
    }

    /// <inheritdoc />
    public void Adicionar(Notificacao notificacao)
    {
        _dbContext.Notificacoes.Add(notificacao);
    }

    private static async Task<PagedResult<T>> PageAsync<T>(IQueryable<T> query, int page, int pageSize, CancellationToken cancellationToken)
    {
        int totalItems = await query.CountAsync(cancellationToken);
        T[] items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToArrayAsync(cancellationToken);
        return new PagedResult<T>(items, page, pageSize, totalItems);
    }
}
