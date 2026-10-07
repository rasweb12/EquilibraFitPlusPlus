using EquilibraFitPlusPlus.Contracts.Notificacoes;
using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Features.Notificacoes;

/// <summary>
/// Maps notification entities.
/// </summary>
internal static class NotificacaoMapper
{
    /// <summary>Maps a notification.</summary>
    public static NotificacaoResponse Map(Notificacao notificacao)
    {
        return new NotificacaoResponse(
            notificacao.Id,
            notificacao.UsuarioId,
            notificacao.Titulo,
            notificacao.Mensagem,
            notificacao.Status.ToString(),
            notificacao.CriadoEm);
    }
}
