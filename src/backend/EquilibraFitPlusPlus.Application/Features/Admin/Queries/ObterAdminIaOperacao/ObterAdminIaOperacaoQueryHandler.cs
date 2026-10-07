using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Features.Admin.Mappings;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ObterAdminIaOperacao;

/// <summary>
/// Handles AI operation overview reads.
/// </summary>
public sealed class ObterAdminIaOperacaoQueryHandler : IRequestHandler<ObterAdminIaOperacaoQuery, Result<AdminIaOperacaoResponse>>
{
    private readonly IAdminIaRepository _adminIaRepository;
    private readonly IAdminIaHealthClient _healthClient;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ObterAdminIaOperacaoQueryHandler(IAdminIaRepository adminIaRepository, IAdminIaHealthClient healthClient)
    {
        _adminIaRepository = adminIaRepository;
        _healthClient = healthClient;
    }

    /// <inheritdoc />
    public async Task<Result<AdminIaOperacaoResponse>> Handle(ObterAdminIaOperacaoQuery request, CancellationToken cancellationToken)
    {
        AdminIaServicoStatusSourceData status = await _healthClient.ObterStatusAsync(cancellationToken);
        AdminIaMetricasSourceData metrics = await _adminIaRepository.ObterMetricasAsync(request.TenantId, request.DataReferencia, cancellationToken);
        IReadOnlyCollection<AdminIaHistoricoSourceData> history = await _adminIaRepository.ListarHistoricoAsync(request.TenantId, 25, cancellationToken);
        AdminIaEnsinoSourceData teaching = await _adminIaRepository.ObterEnsinoAtualAsync(request.TenantId, cancellationToken);

        return Result<AdminIaOperacaoResponse>.Success(AdminIaMapper.Map(status, metrics, history, teaching));
    }
}
