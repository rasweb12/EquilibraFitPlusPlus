using EquilibraFitPlusPlus.Application.Abstractions.Alimentacao;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Mappings;
using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Queries.ObterRegistroAlimentar;

/// <summary>
/// Handles food log detail reads.
/// </summary>
public sealed class ObterRegistroAlimentarQueryHandler : IRequestHandler<ObterRegistroAlimentarQuery, Result<RegistroAlimentarResponse>>
{
    private readonly IAlimentacaoRepository _alimentacaoRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ObterRegistroAlimentarQueryHandler(IAlimentacaoRepository alimentacaoRepository)
    {
        _alimentacaoRepository = alimentacaoRepository;
    }

    /// <inheritdoc />
    public async Task<Result<RegistroAlimentarResponse>> Handle(ObterRegistroAlimentarQuery request, CancellationToken cancellationToken)
    {
        RegistroAlimentar? registro = await _alimentacaoRepository.ObterPorIdAsync(request.TenantId, request.UsuarioId, request.RegistroId, cancellationToken);
        return registro is null
            ? Result<RegistroAlimentarResponse>.Failure(new Error("alimentacao.nao_encontrada", "Não encontramos esse registro alimentar."))
            : Result<RegistroAlimentarResponse>.Success(RegistroAlimentarMapper.Map(registro));
    }
}
