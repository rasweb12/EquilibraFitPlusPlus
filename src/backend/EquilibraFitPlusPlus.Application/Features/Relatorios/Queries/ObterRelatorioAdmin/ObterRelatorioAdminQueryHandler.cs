using EquilibraFitPlusPlus.Application.Abstractions.Relatorios;
using EquilibraFitPlusPlus.Contracts.Relatorios;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Relatorios.Queries.ObterRelatorioAdmin;

/// <summary>
/// Handles administrative report generation.
/// </summary>
public sealed class ObterRelatorioAdminQueryHandler : IRequestHandler<ObterRelatorioAdminQuery, Result<RelatorioAdminResumoResponse>>
{
    private readonly IRelatoriosRepository _relatoriosRepository;

    /// <summary>Initializes the handler.</summary>
    public ObterRelatorioAdminQueryHandler(IRelatoriosRepository relatoriosRepository)
    {
        _relatoriosRepository = relatoriosRepository;
    }

    /// <inheritdoc />
    public async Task<Result<RelatorioAdminResumoResponse>> Handle(ObterRelatorioAdminQuery request, CancellationToken cancellationToken)
    {
        RelatorioAdminSourceData source = await _relatoriosRepository.ObterRelatorioAdminAsync(request.TenantId, request.Inicio, request.Fim, cancellationToken);
        return Result<RelatorioAdminResumoResponse>.Success(new RelatorioAdminResumoResponse(
            request.Inicio,
            request.Fim,
            source.UsuariosAtivos,
            source.NovosUsuarios,
            source.RefeicoesRegistradas,
            source.PlanosGerados,
            source.TreinosAtivos,
            source.SessoesCoach,
            source.ReconhecimentosImagem,
            source.Receita,
            "Relatório administrativo gerado com dados agregados e minimização de informações sensíveis."));
    }
}
