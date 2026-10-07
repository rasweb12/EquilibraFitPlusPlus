using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Marketplace;
using EquilibraFitPlusPlus.Contracts.Marketplace;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Marketplace.Commands.SalvarParceiro;

/// <summary>
/// Handles partner persistence.
/// </summary>
public sealed class SalvarParceiroCommandHandler : IRequestHandler<SalvarParceiroCommand, Result<ParceiroResponse>>
{
    private readonly IMarketplaceRepository _marketplaceRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes the handler.</summary>
    public SalvarParceiroCommandHandler(IMarketplaceRepository marketplaceRepository, IAdminRepository adminRepository, IUnitOfWork unitOfWork)
    {
        _marketplaceRepository = marketplaceRepository;
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<ParceiroResponse>> Handle(SalvarParceiroCommand command, CancellationToken cancellationToken)
    {
        Parceiro? parceiro = command.Request.Id.HasValue
            ? await _marketplaceRepository.ObterParceiroAsync(command.TenantId, command.Request.Id.Value, cancellationToken)
            : null;

        if (command.Request.Id.HasValue && parceiro is null)
        {
            return Result<ParceiroResponse>.Failure(new Error("marketplace.parceiro_nao_encontrado", "Não encontramos esse parceiro."));
        }

        if (parceiro is null)
        {
            parceiro = new Parceiro { TenantId = command.TenantId };
            _marketplaceRepository.AdicionarParceiro(parceiro);
        }

        parceiro.Nome = command.Request.Nome.Trim();
        parceiro.Tipo = Enum.Parse<TipoParceiro>(command.Request.Tipo, ignoreCase: true);
        parceiro.Status = Enum.Parse<StatusParceiro>(command.Request.Status, ignoreCase: true);
        parceiro.EmailContato = string.IsNullOrWhiteSpace(command.Request.EmailContato) ? null : command.Request.EmailContato.Trim();
        parceiro.Documento = string.IsNullOrWhiteSpace(command.Request.Documento) ? null : command.Request.Documento.Trim();

        _adminRepository.AdicionarAuditoria(new Auditoria
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioId,
            Acao = "marketplace.parceiro.salvar",
            Entidade = nameof(Parceiro),
            EntidadeId = parceiro.Id,
            Ip = command.Ip,
            UserAgent = command.UserAgent
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<ParceiroResponse>.Success(MarketplaceMapper.Map(parceiro));
    }
}
