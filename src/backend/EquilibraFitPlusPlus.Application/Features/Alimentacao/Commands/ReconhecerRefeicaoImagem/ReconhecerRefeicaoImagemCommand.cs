using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.ReconhecerRefeicaoImagem;

/// <summary>
/// Command used to recognize a meal image.
/// </summary>
public sealed record ReconhecerRefeicaoImagemCommand(Guid TenantId, Guid UsuarioId, ReconhecerRefeicaoImagemRequest Request)
    : IRequest<Result<RefeicaoImagemReconhecidaResponse>>;
