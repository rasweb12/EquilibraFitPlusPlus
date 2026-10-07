using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.TestarAdminIaEnsino;

/// <summary>
/// Command used to test AI teaching configuration before publication.
/// </summary>
public sealed record TestarAdminIaEnsinoCommand(
    Guid TenantId,
    Guid AdminUsuarioId,
    TestarAdminIaEnsinoRequest Request) : IRequest<Result<TestarAdminIaEnsinoResponse>>;
