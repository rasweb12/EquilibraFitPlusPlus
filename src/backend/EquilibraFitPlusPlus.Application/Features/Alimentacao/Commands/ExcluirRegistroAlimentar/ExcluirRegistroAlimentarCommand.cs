using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.ExcluirRegistroAlimentar;

/// <summary>
/// Command used to soft delete a food log.
/// </summary>
public sealed record ExcluirRegistroAlimentarCommand(Guid TenantId, Guid UsuarioId, Guid RegistroId) : IRequest<Result>;
