using EquilibraFitPlusPlus.Application.Abstractions.Habitos;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Application.Features.Habitos.Mappings;
using EquilibraFitPlusPlus.Contracts.Habitos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Habitos.Queries.ObterHabitosDiarios;

/// <summary>
/// Handles daily habits reads.
/// </summary>
public sealed class ObterHabitosDiariosQueryHandler : IRequestHandler<ObterHabitosDiariosQuery, Result<HabitosDiariosResponse>>
{
    private readonly IHabitosRepository _habitosRepository;
    private readonly IUsuarioRepository _usuarioRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ObterHabitosDiariosQueryHandler(IHabitosRepository habitosRepository, IUsuarioRepository usuarioRepository)
    {
        _habitosRepository = habitosRepository;
        _usuarioRepository = usuarioRepository;
    }

    /// <inheritdoc />
    public async Task<Result<HabitosDiariosResponse>> Handle(ObterHabitosDiariosQuery request, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(request.TenantId, request.UsuarioId, cancellationToken) is null)
        {
            return Result<HabitosDiariosResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        RegistroHabitos? registro = await _habitosRepository.ObterPorDataAsync(request.TenantId, request.UsuarioId, request.Data, cancellationToken);
        HabitosDiariosResponse response = registro is null
            ? HabitosMapper.Empty(request.Data)
            : HabitosMapper.Map(registro, "Hábitos carregados. O importante é continuar.");

        return Result<HabitosDiariosResponse>.Success(response);
    }
}
