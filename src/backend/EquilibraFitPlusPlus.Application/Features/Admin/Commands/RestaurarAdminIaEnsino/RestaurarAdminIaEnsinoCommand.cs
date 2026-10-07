using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.RestaurarAdminIaEnsino;

/// <summary>
/// Command used to restore a previous AI teaching version.
/// </summary>
public sealed record RestaurarAdminIaEnsinoCommand(
    Guid TenantId,
    Guid AdminUsuarioId,
    string? Ip,
    string? UserAgent,
    RestaurarAdminIaEnsinoRequest Request) : IRequest<Result<AdminIaEnsinoResponse>>;
