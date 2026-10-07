using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Lgpd;
using EquilibraFitPlusPlus.Contracts.Lgpd;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Lgpd.Queries.ExportarDadosUsuario;

/// <summary>
/// Handles LGPD data exports.
/// </summary>
public sealed class ExportarDadosUsuarioQueryHandler : IRequestHandler<ExportarDadosUsuarioQuery, Result<LgpdExportResponse>>
{
    private readonly ILgpdRepository _lgpdRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes the handler.</summary>
    public ExportarDadosUsuarioQueryHandler(ILgpdRepository lgpdRepository, IAdminRepository adminRepository, IUnitOfWork unitOfWork)
    {
        _lgpdRepository = lgpdRepository;
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<LgpdExportResponse>> Handle(ExportarDadosUsuarioQuery request, CancellationToken cancellationToken)
    {
        LgpdExportSourceData? source = await _lgpdRepository.ObterExportacaoAsync(request.TenantId, request.UsuarioId, cancellationToken);
        if (source is null)
        {
            return Result<LgpdExportResponse>.Failure(new Error("lgpd.usuario_nao_encontrado", "Não foi possível localizar seus dados para exportação."));
        }

        _adminRepository.AdicionarAuditoria(new Auditoria
        {
            TenantId = request.TenantId,
            UsuarioId = request.UsuarioId,
            Acao = "lgpd.exportar",
            Entidade = nameof(Usuario),
            EntidadeId = request.UsuarioId,
            Ip = request.Ip,
            UserAgent = request.UserAgent
        });
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LgpdExportResponse>.Success(new LgpdExportResponse(
            DateTimeOffset.UtcNow,
            new LgpdUsuarioResponse(
                source.Usuario.Id,
                source.Usuario.Nome,
                source.Usuario.Email,
                source.Usuario.Role.ToString(),
                source.Usuario.Status.ToString()),
            source.PerfilSaude is null
                ? null
                : new LgpdPerfilSaudeResponse(
                    source.PerfilSaude.DataNascimento,
                    source.PerfilSaude.AlturaCm,
                    source.PerfilSaude.PesoAtualKg,
                    source.PerfilSaude.Objetivo.ToString(),
                    source.PerfilSaude.NivelAtividade.ToString(),
                    source.PerfilSaude.DiasTreinoSemana),
            source.Consentimentos.Select(item => new LgpdConsentimentoResponse(
                item.Tipo.ToString(),
                item.Versao,
                item.AceitoEm,
                item.Origem)).ToArray(),
            new LgpdResumoDadosResponse(
                source.RegistrosAlimentares,
                source.Treinos,
                source.Planos,
                source.SessoesCoach,
                source.Notificacoes),
            "Exportacao gerada com seus dados estruturados. Podemos ajudar com correcoes ou exclusao quando desejar."));
    }
}
