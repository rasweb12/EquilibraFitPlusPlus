using EquilibraFitPlusPlus.Application.Abstractions.Relatorios;
using EquilibraFitPlusPlus.Contracts.Relatorios;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Relatorios.Queries.ObterRelatorioUsuario;

/// <summary>
/// Handles user report generation.
/// </summary>
public sealed class ObterRelatorioUsuarioQueryHandler : IRequestHandler<ObterRelatorioUsuarioQuery, Result<RelatorioUsuarioResumoResponse>>
{
    private readonly IRelatoriosRepository _relatoriosRepository;

    /// <summary>Initializes the handler.</summary>
    public ObterRelatorioUsuarioQueryHandler(IRelatoriosRepository relatoriosRepository)
    {
        _relatoriosRepository = relatoriosRepository;
    }

    /// <inheritdoc />
    public async Task<Result<RelatorioUsuarioResumoResponse>> Handle(ObterRelatorioUsuarioQuery request, CancellationToken cancellationToken)
    {
        RelatorioUsuarioSourceData source = await _relatoriosRepository.ObterRelatorioUsuarioAsync(
            request.TenantId,
            request.UsuarioId,
            request.Inicio,
            request.Fim,
            cancellationToken);

        RegistroEvolucao? first = source.Evolucoes.FirstOrDefault();
        RegistroEvolucao? last = source.Evolucoes.LastOrDefault();

        return Result<RelatorioUsuarioResumoResponse>.Success(new RelatorioUsuarioResumoResponse(
            request.Inicio,
            request.Fim,
            source.RegistrosAlimentares.Count,
            source.RegistrosAlimentares.Sum(item => item.CaloriasTotal),
            source.RegistrosAlimentares.Sum(item => item.ProteinaTotalG),
            source.RegistrosAlimentares.Sum(item => item.CarboidratoTotalG),
            source.RegistrosAlimentares.Sum(item => item.GorduraTotalG),
            source.TreinosAtivos.Count,
            first?.PesoKg,
            last?.PesoKg,
            first is null || last is null ? null : last.PesoKg - first.PesoKg,
            "Relatório gerado. Use como orientação tranquila para ajustar os próximos passos."));
    }
}
