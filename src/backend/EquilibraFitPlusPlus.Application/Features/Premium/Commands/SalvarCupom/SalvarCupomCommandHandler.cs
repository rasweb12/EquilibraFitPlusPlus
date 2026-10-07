using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Premium;
using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Premium.Commands.SalvarCupom;

/// <summary>
/// Handles coupon persistence.
/// </summary>
public sealed class SalvarCupomCommandHandler : IRequestHandler<SalvarCupomCommand, Result<CupomResponse>>
{
    private readonly IPremiumRepository _premiumRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes the handler.</summary>
    public SalvarCupomCommandHandler(IPremiumRepository premiumRepository, IAdminRepository adminRepository, IUnitOfWork unitOfWork)
    {
        _premiumRepository = premiumRepository;
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<CupomResponse>> Handle(SalvarCupomCommand command, CancellationToken cancellationToken)
    {
        string code = command.Request.Codigo.Trim().ToUpperInvariant();
        Cupom? cupom = await _premiumRepository.ObterCupomPorCodigoAsync(command.TenantId, code, cancellationToken);
        if (cupom is null)
        {
            cupom = new Cupom { TenantId = command.TenantId, Codigo = code };
            _premiumRepository.AdicionarCupom(cupom);
        }

        cupom.PercentualDesconto = command.Request.PercentualDesconto;
        cupom.ValorDesconto = command.Request.ValorDesconto;
        cupom.ExpiraEm = command.Request.ExpiraEm;
        cupom.UsoMaximo = command.Request.UsoMaximo;

        _adminRepository.AdicionarAuditoria(new Auditoria
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioId,
            Acao = "premium.cupom.salvar",
            Entidade = nameof(Cupom),
            EntidadeId = cupom.Id,
            Ip = command.Ip,
            UserAgent = command.UserAgent
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CupomResponse>.Success(PremiumMapper.Map(cupom));
    }
}
