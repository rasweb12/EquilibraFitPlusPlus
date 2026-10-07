using EquilibraFitPlusPlus.Contracts.Marketplace;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Marketplace.Commands.SalvarParceiro;

/// <summary>
/// Command used to create or update a marketplace partner.
/// </summary>
public sealed record SalvarParceiroCommand(Guid TenantId, Guid UsuarioId, string? Ip, string? UserAgent, SalvarParceiroRequest Request)
    : IRequest<Result<ParceiroResponse>>;
