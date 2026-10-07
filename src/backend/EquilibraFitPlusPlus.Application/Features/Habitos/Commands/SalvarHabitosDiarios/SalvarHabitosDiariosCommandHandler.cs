using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Habitos;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Application.Features.Habitos.Mappings;
using EquilibraFitPlusPlus.Contracts.Habitos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Habitos.Commands.SalvarHabitosDiarios;

/// <summary>
/// Handles daily habits persistence.
/// </summary>
public sealed class SalvarHabitosDiariosCommandHandler : IRequestHandler<SalvarHabitosDiariosCommand, Result<HabitosDiariosResponse>>
{
    private readonly IHabitosRepository _habitosRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public SalvarHabitosDiariosCommandHandler(
        IHabitosRepository habitosRepository,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _habitosRepository = habitosRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<HabitosDiariosResponse>> Handle(SalvarHabitosDiariosCommand command, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken) is null)
        {
            return Result<HabitosDiariosResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        DateOnly data = command.Request.Data ?? DateOnly.FromDateTime(DateTime.UtcNow);
        RegistroHabitos? registro = await _habitosRepository.ObterPorDataAsync(command.TenantId, command.UsuarioId, data, cancellationToken);
        if (registro is null)
        {
            registro = new RegistroHabitos
            {
                TenantId = command.TenantId,
                UsuarioId = command.UsuarioId,
                Data = data
            };

            _habitosRepository.Adicionar(registro);
        }

        registro.AguaMl = command.Request.AguaMl;
        registro.MetaAguaMl = command.Request.MetaAguaMl;
        registro.SonoHoras = command.Request.SonoHoras;
        registro.MetaSonoHoras = command.Request.MetaSonoHoras;
        registro.Humor = command.Request.Humor;
        registro.MeditacaoRealizada = command.Request.MeditacaoRealizada;
        registro.AlongamentoRealizado = command.Request.AlongamentoRealizado;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<HabitosDiariosResponse>.Success(HabitosMapper.Map(
            registro,
            "Hábitos sincronizados. Sem problemas, seguimos ajustando no seu ritmo."));
    }
}
