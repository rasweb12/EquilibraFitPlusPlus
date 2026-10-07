using EquilibraFitPlusPlus.Contracts.AiCoach;
using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Features.AiCoach.Mappings;

/// <summary>
/// Maps AI Coach entities to API contracts.
/// </summary>
internal static class CoachSessionMapper
{
    /// <summary>
    /// Maps a chat session.
    /// </summary>
    public static CoachSessionResponse Map(ChatSession session)
    {
        return new CoachSessionResponse(
            session.Id,
            session.Titulo,
            session.CriadoEm,
            session.Mensagens
                .OrderBy(message => message.CriadoEm)
                .Select(MapMessage)
                .ToArray());
    }

    /// <summary>
    /// Maps a chat message.
    /// </summary>
    public static CoachMessageResponse MapMessage(ChatMessage message)
    {
        return new CoachMessageResponse(message.Id, message.Role, message.Conteudo, message.CriadoEm, message.ModeloIa);
    }
}
