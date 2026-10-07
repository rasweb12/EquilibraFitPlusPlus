using EquilibraFitPlusPlus.Contracts.Habitos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Habitos.Commands.SalvarHabitosDiarios;

/// <summary>
/// Command used to save daily habits.
/// </summary>
public sealed record SalvarHabitosDiariosCommand(
    Guid TenantId,
    Guid UsuarioId,
    SalvarHabitosDiariosRequest Request) : IRequest<Result<HabitosDiariosResponse>>;
