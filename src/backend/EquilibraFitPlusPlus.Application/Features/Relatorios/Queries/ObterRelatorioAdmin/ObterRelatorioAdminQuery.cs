using EquilibraFitPlusPlus.Contracts.Relatorios;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Relatorios.Queries.ObterRelatorioAdmin;

/// <summary>
/// Query used to get an administrative report.
/// </summary>
public sealed record ObterRelatorioAdminQuery(Guid TenantId, DateOnly Inicio, DateOnly Fim)
    : IRequest<Result<RelatorioAdminResumoResponse>>;
