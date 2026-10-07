using EquilibraFitPlusPlus.Application.Abstractions.Alimentacao;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Application.Features.Alimentacao.Mappings;
using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.CriarRegistroAlimentar;

/// <summary>
/// Handles food log creation.
/// </summary>
public sealed class CriarRegistroAlimentarCommandHandler : IRequestHandler<CriarRegistroAlimentarCommand, Result<RegistroAlimentarResponse>>
{
    private readonly IAlimentacaoRepository _alimentacaoRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public CriarRegistroAlimentarCommandHandler(
        IAlimentacaoRepository alimentacaoRepository,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _alimentacaoRepository = alimentacaoRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<RegistroAlimentarResponse>> Handle(CriarRegistroAlimentarCommand command, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken) is null)
        {
            return Result<RegistroAlimentarResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        var tipoRefeicao = Enum.Parse<TipoRefeicao>(command.Request.TipoRefeicao, ignoreCase: true);
        var itens = command.Request.Itens
            .Select(item => new ItemAlimentar
            {
                TenantId = command.TenantId,
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

        var registro = new RegistroAlimentar
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioId,
            DataHora = command.Request.DataHora,
            TipoRefeicao = tipoRefeicao,
            Origem = OrigemRegistroAlimentar.Manual,
            ConfirmadoPeloUsuario = command.Request.ConfirmadoPeloUsuario,
            CaloriasTotal = itens.Sum(item => item.Calorias),
            ProteinaTotalG = itens.Sum(item => item.ProteinaG),
            CarboidratoTotalG = itens.Sum(item => item.CarboidratoG),
            GorduraTotalG = itens.Sum(item => item.GorduraG),
            Itens = itens
        };

        _alimentacaoRepository.Adicionar(registro);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RegistroAlimentarResponse>.Success(RegistroAlimentarMapper.Map(registro));
    }
}
