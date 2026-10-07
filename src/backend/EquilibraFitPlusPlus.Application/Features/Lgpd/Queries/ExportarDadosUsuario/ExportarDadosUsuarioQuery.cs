using EquilibraFitPlusPlus.Contracts.Lgpd;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Lgpd.Queries.ExportarDadosUsuario;

/// <summary>
/// Query used to export user data for LGPD access and portability.
/// </summary>
public sealed record ExportarDadosUsuarioQuery(Guid TenantId, Guid UsuarioId, string? Ip, string? UserAgent)
    : IRequest<Result<LgpdExportResponse>>;
