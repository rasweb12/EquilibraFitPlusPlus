using EquilibraFitPlusPlus.Contracts.Planos;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Planos.Commands.SubstituirPlanoRefeicao;

/// <summary>
/// Replaces one active-plan meal with a user-confirmed generated alternative.
/// </summary>
public sealed record SubstituirPlanoRefeicaoCommand(
    Guid TenantId,
    Guid UsuarioId,
    Guid PlanoId,
    Guid RefeicaoId,
    SubstituirPlanoRefeicaoRequest Request) : IRequest<Result>;

