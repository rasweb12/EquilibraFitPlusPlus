using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.EstimarRefeicaoTexto;

/// <summary>
/// Command used to estimate a meal from text.
/// </summary>
public sealed record EstimarRefeicaoTextoCommand(
    Guid TenantId,
    Guid UsuarioId,
    EstimarRefeicaoTextoRequest Request) : IRequest<Result<RefeicaoTextoEstimadaResponse>>;
