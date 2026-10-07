using EquilibraFitPlusPlus.Application.Abstractions.Relatorios;
using EquilibraFitPlusPlus.Contracts.Gamificacao;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Gamificacao.Queries.ObterGamificacaoAdmin;

/// <summary>
/// Handles administrative gamification summary.
/// </summary>
public sealed class ObterGamificacaoAdminQueryHandler : IRequestHandler<ObterGamificacaoAdminQuery, Result<GamificacaoAdminResumoResponse>>
{
    private readonly IRelatoriosRepository _relatoriosRepository;

    /// <summary>Initializes the handler.</summary>
    public ObterGamificacaoAdminQueryHandler(IRelatoriosRepository relatoriosRepository)
    {
        _relatoriosRepository = relatoriosRepository;
    }

    /// <inheritdoc />
    public async Task<Result<GamificacaoAdminResumoResponse>> Handle(ObterGamificacaoAdminQuery request, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        IReadOnlyCollection<UsuarioEngajadoSourceData> users = await _relatoriosRepository.ListarUsuariosEngajadosAsync(
            request.TenantId,
            today.AddDays(-29),
            today,
            10,
            cancellationToken);

        UsuarioEngajadoResponse[] ranking = users
            .Select(item => new UsuarioEngajadoResponse(
                item.UsuarioId,
                item.Nome,
                (item.RegistrosAlimentares * 5) + (item.TreinosAtivos * 20),
                item.RegistrosAlimentares > 0 ? 1 : 0))
            .ToArray();

        return Result<GamificacaoAdminResumoResponse>.Success(new GamificacaoAdminResumoResponse(
            ranking.Count(item => item.SequenciaDias > 0),
            ranking.Sum(item => item.Pontos >= 50 ? 1 : 0),
            ranking));
    }
}
