using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.ExcluirTreino;

/// <summary>
/// Command used to soft delete a workout.
/// </summary>
public sealed record ExcluirTreinoCommand(Guid TenantId, Guid UsuarioId, Guid TreinoId) : IRequest<Result>;
