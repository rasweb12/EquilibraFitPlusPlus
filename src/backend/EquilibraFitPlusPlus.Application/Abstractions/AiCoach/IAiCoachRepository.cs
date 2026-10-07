using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;

namespace EquilibraFitPlusPlus.Application.Abstractions.AiCoach;

/// <summary>
/// Provides persistence operations for AI Coach sessions.
/// </summary>
public interface IAiCoachRepository
{
    /// <summary>
    /// Adds a chat session.
    /// </summary>
    void AdicionarSessao(ChatSession session);

    /// <summary>
    /// Gets a chat session by owner and identifier.
    /// </summary>
    Task<ChatSession?> ObterSessaoAsync(Guid tenantId, Guid usuarioId, Guid sessaoId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists chat sessions for a user.
    /// </summary>
    Task<PagedResult<ChatSession>> ListarSessoesAsync(Guid tenantId, Guid usuarioId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Gets contextual data used by AI Coach.
    /// </summary>
    Task<AiCoachUserContext> ObterContextoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Lists confirmed coach memories for a user.
    /// </summary>
    Task<IReadOnlyCollection<AiCoachMemory>> ListarMemoriasAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets one coach memory by stable key.
    /// </summary>
    Task<AiCoachMemory?> ObterMemoriaAsync(Guid tenantId, Guid usuarioId, string chave, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a coach memory.
    /// </summary>
    void AdicionarMemoria(AiCoachMemory memoria);

    /// <summary>
    /// Adds sanitized AI execution metadata.
    /// </summary>
    void AdicionarExecutionLog(AiExecutionLog log);
}

/// <summary>
/// User context used by the AI Coach.
/// </summary>
public sealed record AiCoachUserContext(
    PerfilSaude? Perfil,
    PlanoUsuario? PlanoAtual,
    RegistroEvolucao? UltimaEvolucao,
    IReadOnlyCollection<RegistroAlimentar> RegistrosRecentes);
