using EquilibraFitPlusPlus.Contracts.Relatorios;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Relatorios.Queries.ObterRelatorioUsuario;

/// <summary>
/// Query used to get a user report.
/// </summary>
public sealed record ObterRelatorioUsuarioQuery(Guid TenantId, Guid UsuarioId, DateOnly Inicio, DateOnly Fim)
    : IRequest<Result<RelatorioUsuarioResumoResponse>>;
