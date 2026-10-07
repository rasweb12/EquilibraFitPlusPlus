using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AtualizarTreino;

/// <summary>
/// Command used to update workout cycle metadata.
/// </summary>
public sealed record AtualizarTreinoCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid TreinoId,
    AtualizarTreinoRequest Request) : IRequest<Result<TreinoUsuarioResponse>>;
