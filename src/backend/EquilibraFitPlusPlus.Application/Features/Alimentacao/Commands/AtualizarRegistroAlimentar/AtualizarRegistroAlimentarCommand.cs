using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.AtualizarRegistroAlimentar;

/// <summary>
/// Command used to update a food log.
/// </summary>
public sealed record AtualizarRegistroAlimentarCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid RegistroId,
    AtualizarRegistroAlimentarRequest Request) : IRequest<Result<RegistroAlimentarResponse>>;
