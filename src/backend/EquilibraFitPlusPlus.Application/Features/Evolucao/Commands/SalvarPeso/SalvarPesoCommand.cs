using EquilibraFitPlusPlus.Contracts.Evolucao;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Evolucao.Commands.SalvarPeso;

/// <summary>
/// Command used to save a body weight progress log.
/// </summary>
public sealed record SalvarPesoCommand(
    Guid TenantId,
    Guid UsuarioId,
    SalvarPesoRequest Request) : IRequest<Result<RegistroEvolucaoResponse>>;
