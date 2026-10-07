using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Premium.Commands.SalvarCupom;

/// <summary>
/// Command used to create or update a coupon.
/// </summary>
public sealed record SalvarCupomCommand(Guid TenantId, Guid UsuarioId, string? Ip, string? UserAgent, SalvarCupomRequest Request)
    : IRequest<Result<CupomResponse>>;
