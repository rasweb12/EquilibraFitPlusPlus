using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.ReconhecerRotuloImagem;

/// <summary>
/// Command used to recognize a nutrition label image.
/// </summary>
public sealed record ReconhecerRotuloImagemCommand(Guid TenantId, Guid UsuarioId, ReconhecerRotuloImagemRequest Request)
    : IRequest<Result<RotuloNutricionalReconhecidoResponse>>;
