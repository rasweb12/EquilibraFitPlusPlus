using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;

namespace EquilibraFitPlusPlus.Application.Abstractions.Alimentacao;

/// <summary>
/// Provides persistence operations for food logs.
/// </summary>
public interface IAlimentacaoRepository
{
    /// <summary>
    /// Adds an AI meal image analysis.
    /// </summary>
    void AdicionarAnalise(AnaliseRefeicaoImagem analise);

    /// <summary>
    /// Adds a food log.
    /// </summary>
    void Adicionar(RegistroAlimentar registroAlimentar);

    /// <summary>
    /// Removes all food items linked to a food log.
    /// </summary>
    void RemoverItens(RegistroAlimentar registroAlimentar);

    /// <summary>
    /// Gets a food log by owner and identifier.
    /// </summary>
    Task<RegistroAlimentar?> ObterPorIdAsync(Guid tenantId, Guid usuarioId, Guid registroId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists food logs for a user.
    /// </summary>
    Task<PagedResult<RegistroAlimentar>> ListarAsync(
        Guid tenantId,
        Guid usuarioId,
        DateOnly? inicio,
        DateOnly? fim,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
