using EquilibraFitPlusPlus.Contracts.Lgpd;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Lgpd.Commands.SolicitarExclusaoLgpd;

/// <summary>
/// Command used to record an LGPD deletion request.
/// </summary>
public sealed record SolicitarExclusaoLgpdCommand(Guid TenantId, Guid UsuarioId, string? Ip, string? UserAgent, SolicitarExclusaoLgpdRequest Request)
    : IRequest<Result<SolicitacaoExclusaoLgpdResponse>>;
