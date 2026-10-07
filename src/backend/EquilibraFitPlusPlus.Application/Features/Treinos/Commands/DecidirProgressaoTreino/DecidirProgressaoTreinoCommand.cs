using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.DecidirProgressaoTreino;

/// <summary>
/// Command used to apply or keep a progression suggestion.
/// </summary>
public sealed record DecidirProgressaoTreinoCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid SugestaoId,
    DecidirProgressaoTreinoRequest Request) : IRequest<Result<TreinoProgressaoSugestaoResponse>>;
