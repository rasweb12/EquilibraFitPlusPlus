using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.PublicarAdminIaEnsino;

/// <summary>
/// Command used to publish AI teaching configuration.
/// </summary>
public sealed record PublicarAdminIaEnsinoCommand(
    Guid TenantId,
    Guid AdminUsuarioId,
    string? Ip,
    string? UserAgent,
    PublicarAdminIaEnsinoRequest Request) : IRequest<Result<AdminIaEnsinoResponse>>;
