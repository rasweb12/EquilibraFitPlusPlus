using EquilibraFitPlusPlus.Application.Abstractions.Alimentacao;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Mappings;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.AtualizarRegistroAlimentar;

/// <summary>
/// Handles food log updates.
/// </summary>
public sealed class AtualizarRegistroAlimentarCommandHandler : IRequestHandler<AtualizarRegistroAlimentarCommand, Result<Contracts.Alimentacao.RegistroAlimentarResponse>>
{
    private readonly IAlimentacaoRepository _alimentacaoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public AtualizarRegistroAlimentarCommandHandler(IAlimentacaoRepository alimentacaoRepository, IUnitOfWork unitOfWork)
    {
        _alimentacaoRepository = alimentacaoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<Contracts.Alimentacao.RegistroAlimentarResponse>> Handle(AtualizarRegistroAlimentarCommand command, CancellationToken cancellationToken)
    {
        RegistroAlimentar? registro = await _alimentacaoRepository.ObterPorIdAsync(command.TenantId, command.UsuarioId, command.RegistroId, cancellationToken);
        if (registro is null)
        {
            return Result<Contracts.Alimentacao.RegistroAlimentarResponse>.Failure(new Error("alimentacao.nao_encontrada", "Registro alimentar não encontrado. Podemos atualizar a lista e tentar novamente."));
        }

        var tipoRefeicao = Enum.Parse<TipoRefeicao>(command.Request.TipoRefeicao, ignoreCase: true);
        var itens = command.Request.Itens
            .Select(item => new ItemAlimentar
            {
                TenantId = command.TenantId,
                RegistroAlimentarId = registro.Id,
                Nome = item.Nome.Trim(),
                Quantidade = item.Quantidade,
                Unidade = item.Unidade.Trim(),
                Calorias = item.Calorias,
                ProteinaG = item.ProteinaG,
                CarboidratoG = item.CarboidratoG,
                GorduraG = item.GorduraG,
                FonteNutricional = string.IsNullOrWhiteSpace(item.FonteNutricional) ? null : item.FonteNutricional.Trim()
            })
            .ToArray();

        _alimentacaoRepository.RemoverItens(registro);
        foreach (ItemAlimentar item in itens)
        {
            registro.Itens.Add(item);
        }

        registro.DataHora = command.Request.DataHora;
        registro.TipoRefeicao = tipoRefeicao;
        registro.ConfirmadoPeloUsuario = command.Request.ConfirmadoPeloUsuario;
        registro.CaloriasTotal = itens.Sum(item => item.Calorias);
        registro.ProteinaTotalG = itens.Sum(item => item.ProteinaG);
        registro.CarboidratoTotalG = itens.Sum(item => item.CarboidratoG);
        registro.GorduraTotalG = itens.Sum(item => item.GorduraG);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (PersistenceConflictException)
        {
            return Result<Contracts.Alimentacao.RegistroAlimentarResponse>.Failure(
                new Error(
                    "sync_conflict",
                    "Esta refeição foi alterada por outra operação. Atualize a lista e tente novamente."));
        }

        return Result<Contracts.Alimentacao.RegistroAlimentarResponse>.Success(RegistroAlimentarMapper.Map(registro));
    }
}
