using EquilibraFitPlusPlus.Application.Abstractions.Relatorios;
using EquilibraFitPlusPlus.Contracts.Gamificacao;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Gamificacao.Queries.ObterGamificacaoUsuario;

/// <summary>
/// Handles user gamification summary.
/// </summary>
public sealed class ObterGamificacaoUsuarioQueryHandler : IRequestHandler<ObterGamificacaoUsuarioQuery, Result<GamificacaoResumoResponse>>
{
    private readonly IRelatoriosRepository _relatoriosRepository;

    /// <summary>Initializes the handler.</summary>
    public ObterGamificacaoUsuarioQueryHandler(IRelatoriosRepository relatoriosRepository)
    {
        _relatoriosRepository = relatoriosRepository;
    }

    /// <inheritdoc />
    public async Task<Result<GamificacaoResumoResponse>> Handle(ObterGamificacaoUsuarioQuery request, CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        RelatorioUsuarioSourceData source = await _relatoriosRepository.ObterRelatorioUsuarioAsync(
            request.TenantId,
            request.UsuarioId,
            today.AddDays(-29),
            today,
            cancellationToken);

        int foodDays = source.RegistrosAlimentares.Select(item => DateOnly.FromDateTime(item.DataHora.UtcDateTime)).Distinct().Count();
        int points = (source.RegistrosAlimentares.Count * 5) + (source.TreinosAtivos.Count * 20) + (source.Evolucoes.Count * 15);
        int streak = CalculateStreak(source.RegistrosAlimentares.Select(item => DateOnly.FromDateTime(item.DataHora.UtcDateTime)).Distinct().ToHashSet(), today);
        string level = points switch
        {
            >= 500 => "Equilíbrio consistente",
            >= 200 => "Ritmo em construção",
            >= 50 => "Primeiros passos",
            _ => "Início tranquilo"
        };

        ConquistaResponse[] achievements =
        [
            new("primeiro_registro", "Primeiro registro", "Registrar uma refeição.", source.RegistrosAlimentares.Count > 0),
            new("semana_presente", "Semana presente", "Ter registros em 5 dias dos últimos 7.", foodDays >= 5),
            new("plano_em_acao", "Plano em ação", "Manter um treino ativo.", source.TreinosAtivos.Count > 0),
            new("evolucao_monitorada", "Evolução monitorada", "Registrar evolução corporal.", source.Evolucoes.Count > 0)
        ];

        return Result<GamificacaoResumoResponse>.Success(new GamificacaoResumoResponse(
            points,
            level,
            streak,
            achievements,
            "Conquistas mostram constância, não perfeição. O importante é continuar."));
    }

    private static int CalculateStreak(IReadOnlySet<DateOnly> activeDays, DateOnly today)
    {
        int streak = 0;
        for (DateOnly day = today; activeDays.Contains(day); day = day.AddDays(-1))
        {
            streak++;
        }

        return streak;
    }
}
