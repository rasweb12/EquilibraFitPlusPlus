using EquilibraFitPlusPlus.Contracts.Planos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Planos.Commands.GerarPlanoAlimentar;

/// <summary>
/// Command used to generate a personalized diet plan.
/// </summary>
public sealed record GerarPlanoAlimentarCommand(Guid TenantId, Guid UsuarioId, GerarPlanoAlimentarRequest Request)
    : IRequest<Result<PlanoAlimentarGeradoResponse>>;
