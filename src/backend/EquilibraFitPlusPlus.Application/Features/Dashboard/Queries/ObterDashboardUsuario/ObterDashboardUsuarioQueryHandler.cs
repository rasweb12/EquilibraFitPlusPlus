using EquilibraFitPlusPlus.Application.Abstractions.Dashboard;
using EquilibraFitPlusPlus.Application.Common.Health;
using EquilibraFitPlusPlus.Application.Common.Serialization;
using EquilibraFitPlusPlus.Contracts.Dashboard;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Dashboard.Queries.ObterDashboardUsuario;

/// <summary>
/// Handles user dashboard reads.
/// </summary>
public sealed class ObterDashboardUsuarioQueryHandler : IRequestHandler<ObterDashboardUsuarioQuery, Result<DashboardUsuarioResponse>>
{
    private readonly IDashboardRepository _dashboardRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ObterDashboardUsuarioQueryHandler(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    /// <inheritdoc />
    public async Task<Result<DashboardUsuarioResponse>> Handle(ObterDashboardUsuarioQuery request, CancellationToken cancellationToken)
    {
        DashboardSourceData source = await _dashboardRepository.ObterResumoAsync(request.TenantId, request.UsuarioId, request.Data, cancellationToken);

        decimal caloriasConsumidas = source.RegistrosAlimentares.Sum(registro => registro.CaloriasTotal);
        decimal proteina = source.RegistrosAlimentares.Sum(registro => registro.ProteinaTotalG);
        decimal carboidrato = source.RegistrosAlimentares.Sum(registro => registro.CarboidratoTotalG);
        decimal gordura = source.RegistrosAlimentares.Sum(registro => registro.GorduraTotalG);
        decimal? metaCalorias = source.PlanoAtual?.CaloriasDia;
        decimal? saldoCalorico = metaCalorias - caloriasConsumidas;

        var response = new DashboardUsuarioResponse(
            request.Data,
            CreateSupportMessage(source.Perfil, metaCalorias, saldoCalorico, source.RegistrosAlimentares.Count),
            MapPerfil(source.Perfil),
            new DashboardAlimentacaoResponse(
                caloriasConsumidas,
                proteina,
                carboidrato,
                gordura,
                source.RegistrosAlimentares.Count,
                metaCalorias,
                saldoCalorico),
            new DashboardTreinoResponse(
                source.TreinosAtivos.Count,
                source.TreinosAtivos.Sum(treino => treino.FrequenciaSemanal),
                source.TreinosAtivos.OrderBy(treino => treino.CriadoEm).FirstOrDefault()?.Nome),
            new DashboardEvolucaoResponse(
                source.UltimaEvolucao?.PesoKg,
                source.UltimaEvolucao?.Data,
                source.UltimaEvolucao is null || source.EvolucaoAnterior is null
                    ? null
                    : source.UltimaEvolucao.PesoKg - source.EvolucaoAnterior.PesoKg),
            source.PlanoAtual is null
                ? null
                : new DashboardPlanoResponse(
                    source.PlanoAtual.Id,
                    source.PlanoAtual.CaloriasDia,
                    source.Perfil?.Objetivo.ToString() ?? ObjetivoSaude.Manutencao.ToString(),
                    source.PlanoAtual.Explicacao,
                    CreateMealSuggestions(source.Perfil, source.PlanoAtual)));

        return Result<DashboardUsuarioResponse>.Success(response);
    }

    private static DashboardPerfilResponse? MapPerfil(PerfilSaude? perfil)
    {
        if (perfil is null)
        {
            return null;
        }

        return new DashboardPerfilResponse(
            perfil.PesoAtualKg,
            perfil.AlturaCm,
            HealthMetrics.CalculateImc(perfil.PesoAtualKg, perfil.AlturaCm),
            perfil.Objetivo.ToString());
    }

    private static string CreateSupportMessage(PerfilSaude? perfil, decimal? metaCalorias, decimal? saldoCalorico, int refeicoesRegistradas)
    {
        if (perfil is null)
        {
            return "Vamos começar pelo questionário inicial para personalizar sua experiência com segurança.";
        }

        if (refeicoesRegistradas == 0)
        {
            return "Quando quiser, registre uma refeição. Pequenos registros já ajudam a ajustar melhor sua rotina.";
        }

        if (metaCalorias.HasValue && saldoCalorico < 0)
        {
            return "Consumiu um pouco mais hoje. Sem problemas, podemos ajustar naturalmente os próximos dias.";
        }

        return "O importante e continuar. Seu dashboard esta acompanhando o dia no seu ritmo.";
    }

    private static IReadOnlyCollection<DashboardSugestaoRefeicaoResponse> CreateMealSuggestions(PerfilSaude? perfil, PlanoUsuario plano)
    {
        string preferences = CreateReadableList(perfil?.PreferenciasJson, "suas preferências");
        string restrictions = CreateReadableList(perfil?.RestricoesJson, "restrições informadas");

        return
        [
            new DashboardSugestaoRefeicaoResponse(
                "Café da manhã",
                $"Proteína leve, fruta ou carboidrato simples considerando {preferences}.",
                (int)Math.Round(plano.CaloriasDia * 0.25m)),
            new DashboardSugestaoRefeicaoResponse(
                "Almoço",
                $"Base com legumes, proteína e carboidrato ajustado; respeitar {restrictions}.",
                (int)Math.Round(plano.CaloriasDia * 0.35m)),
            new DashboardSugestaoRefeicaoResponse(
                "Jantar",
                $"Opção com boa saciedade e rotina tranquila, usando {preferences} quando fizer sentido.",
                (int)Math.Round(plano.CaloriasDia * 0.30m)),
            new DashboardSugestaoRefeicaoResponse(
                "Lanche opcional",
                "Opção simples para fome entre refeições, sem obrigatoriedade.",
                (int)Math.Round(plano.CaloriasDia * 0.10m))
        ];
    }

    private static string CreateReadableList(string? json, string fallback)
    {
        string[] items = JsonStringCollection.Deserialize(json)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Take(4)
            .ToArray();

        return items.Length == 0 ? fallback : string.Join(", ", items);
    }
}
