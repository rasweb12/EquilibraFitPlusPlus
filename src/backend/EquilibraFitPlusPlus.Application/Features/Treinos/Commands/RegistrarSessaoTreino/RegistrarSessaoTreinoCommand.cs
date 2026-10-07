using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Treinos.Commands.RegistrarSessaoTreino;

/// <summary>
/// Command used to register a real workout execution session.
/// </summary>
public sealed record RegistrarSessaoTreinoCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid TreinoId,
    RegistrarSessaoTreinoRequest Request) : IRequest<Result<TreinoSessaoResponse>>;
