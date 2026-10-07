using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Queries.ObterRegistroAlimentar;

/// <summary>
/// Query used to get a food log by id.
/// </summary>
public sealed record ObterRegistroAlimentarQuery(Guid TenantId, Guid UsuarioId, Guid RegistroId) : IRequest<Result<RegistroAlimentarResponse>>;
