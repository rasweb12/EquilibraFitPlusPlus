using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.CriarRegistroAlimentar;

/// <summary>
/// Command used to create a food log.
/// </summary>
public sealed record CriarRegistroAlimentarCommand(Guid TenantId, Guid UsuarioId, CriarRegistroAlimentarRequest Request)
    : IRequest<Result<RegistroAlimentarResponse>>;
