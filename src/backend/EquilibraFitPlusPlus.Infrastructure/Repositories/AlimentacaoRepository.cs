using EquilibraFitPlusPlus.Application.Abstractions.Alimentacao;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Shared.Pagination;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Repositories;

/// <summary>
/// Entity Framework implementation for food logs.
/// </summary>
public sealed class AlimentacaoRepository : IAlimentacaoRepository
{
    private readonly EquilibraFitPlusPlusDbContext _dbContext;

    /// <summary>
    /// Initializes the repository.
    /// </summary>
    public AlimentacaoRepository(EquilibraFitPlusPlusDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public void AdicionarAnalise(AnaliseRefeicaoImagem analise)
    {
        _dbContext.AnalisesRefeicaoImagem.Add(analise);
    }

    /// <inheritdoc />
    public void Adicionar(RegistroAlimentar registroAlimentar)
    {
        _dbContext.RegistrosAlimentares.Add(registroAlimentar);
    }

    /// <inheritdoc />
    public void RemoverItens(RegistroAlimentar registroAlimentar)
    {
        _dbContext.ItensAlimentares.RemoveRange(registroAlimentar.Itens);
        registroAlimentar.Itens.Clear();
    }

    /// <inheritdoc />
    public Task<RegistroAlimentar?> ObterPorIdAsync(Guid tenantId, Guid usuarioId, Guid registroId, CancellationToken cancellationToken)
    {
        return _dbContext.RegistrosAlimentares
            .Include(registro => registro.Itens)
            .FirstOrDefaultAsync(
                registro => registro.TenantId == tenantId && registro.UsuarioId == usuarioId && registro.Id == registroId,
                cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResult<RegistroAlimentar>> ListarAsync(
        Guid tenantId,
        Guid usuarioId,
        DateOnly? inicio,
        DateOnly? fim,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IQueryable<RegistroAlimentar> query = _dbContext.RegistrosAlimentares
            .Include(registro => registro.Itens)
            .Where(registro => registro.TenantId == tenantId && registro.UsuarioId == usuarioId);

        if (inicio.HasValue)
        {
            DateTimeOffset start = ToUtcStart(inicio.Value);
            query = query.Where(registro => registro.DataHora >= start);
        }

        if (fim.HasValue)
        {
            DateTimeOffset endExclusive = ToUtcStart(fim.Value.AddDays(1));
            query = query.Where(registro => registro.DataHora < endExclusive);
        }

        int totalItems = await query.CountAsync(cancellationToken);
        RegistroAlimentar[] items = await query
            .OrderByDescending(registro => registro.DataHora)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);

        return new PagedResult<RegistroAlimentar>(items, page, pageSize, totalItems);
    }

    private static DateTimeOffset ToUtcStart(DateOnly date)
    {
        return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
    }
}
