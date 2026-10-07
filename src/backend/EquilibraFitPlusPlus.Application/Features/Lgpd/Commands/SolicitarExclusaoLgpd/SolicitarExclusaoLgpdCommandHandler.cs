using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Contracts.Lgpd;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Lgpd.Commands.SolicitarExclusaoLgpd;

/// <summary>
/// Handles LGPD deletion request protocol creation.
/// </summary>
public sealed class SolicitarExclusaoLgpdCommandHandler : IRequestHandler<SolicitarExclusaoLgpdCommand, Result<SolicitacaoExclusaoLgpdResponse>>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes the handler.</summary>
    public SolicitarExclusaoLgpdCommandHandler(IAdminRepository adminRepository, IUnitOfWork unitOfWork)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<SolicitacaoExclusaoLgpdResponse>> Handle(SolicitarExclusaoLgpdCommand command, CancellationToken cancellationToken)
    {
        var auditoria = new Auditoria
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioId,
            Acao = "lgpd.exclusao.solicitada",
            Entidade = nameof(Usuario),
            EntidadeId = command.UsuarioId,
            Ip = command.Ip,
            UserAgent = command.UserAgent
        };

        _adminRepository.AdicionarAuditoria(auditoria);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<SolicitacaoExclusaoLgpdResponse>.Success(new SolicitacaoExclusaoLgpdResponse(
            auditoria.Id,
            auditoria.CriadoEm,
            "Solicitação registrada. Vamos processar com segurança, respeitando prazos legais e trilhas de auditoria."));
    }
}
