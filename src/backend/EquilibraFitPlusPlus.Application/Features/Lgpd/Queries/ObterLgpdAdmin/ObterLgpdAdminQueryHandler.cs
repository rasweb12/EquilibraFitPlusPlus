using EquilibraFitPlusPlus.Application.Abstractions.Lgpd;
using EquilibraFitPlusPlus.Contracts.Lgpd;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Lgpd.Queries.ObterLgpdAdmin;

/// <summary>
/// Handles administrative LGPD summary.
/// </summary>
public sealed class ObterLgpdAdminQueryHandler : IRequestHandler<ObterLgpdAdminQuery, Result<LgpdAdminResumoResponse>>
{
    private readonly ILgpdRepository _lgpdRepository;

    /// <summary>Initializes the handler.</summary>
    public ObterLgpdAdminQueryHandler(ILgpdRepository lgpdRepository)
    {
        _lgpdRepository = lgpdRepository;
    }

    /// <inheritdoc />
    public async Task<Result<LgpdAdminResumoResponse>> Handle(ObterLgpdAdminQuery request, CancellationToken cancellationToken)
    {
        LgpdAdminSourceData source = await _lgpdRepository.ObterResumoAdminAsync(request.TenantId, cancellationToken);
        return Result<LgpdAdminResumoResponse>.Success(new LgpdAdminResumoResponse(
            source.UsuariosAtivos,
            source.ConsentimentosRegistrados,
            source.ExportacoesUltimos30Dias,
            source.SolicitacoesExclusaoAbertas,
            "LGPD operacional com exportacao auditada e solicitacoes rastreaveis."));
    }
}
