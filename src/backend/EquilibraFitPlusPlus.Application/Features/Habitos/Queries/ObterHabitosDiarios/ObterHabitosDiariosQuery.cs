using EquilibraFitPlusPlus.Contracts.Habitos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Habitos.Queries.ObterHabitosDiarios;

/// <summary>
/// Query used to get daily habits.
/// </summary>
public sealed record ObterHabitosDiariosQuery(Guid TenantId, Guid UsuarioId, DateOnly Data) : IRequest<Result<HabitosDiariosResponse>>;
