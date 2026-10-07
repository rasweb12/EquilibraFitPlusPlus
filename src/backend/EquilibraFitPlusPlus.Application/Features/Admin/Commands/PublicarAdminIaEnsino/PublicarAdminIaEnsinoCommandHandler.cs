using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Features.Admin.Mappings;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.PublicarAdminIaEnsino;

/// <summary>
/// Handles AI teaching publication.
/// </summary>
public sealed class PublicarAdminIaEnsinoCommandHandler : IRequestHandler<PublicarAdminIaEnsinoCommand, Result<AdminIaEnsinoResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAdminIaRepository _adminIaRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public PublicarAdminIaEnsinoCommandHandler(IAdminIaRepository adminIaRepository, IAdminRepository adminRepository, IUnitOfWork unitOfWork)
    {
        _adminIaRepository = adminIaRepository;
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<AdminIaEnsinoResponse>> Handle(PublicarAdminIaEnsinoCommand command, CancellationToken cancellationToken)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string version = "v" + now.ToString("yyyyMMddHHmmss");
        var teaching = new AdminIaEnsinoSourceData(
            version,
            command.Request.InstrucoesCoach.Trim(),
            command.Request.RegrasAlimentacao.Trim(),
            command.Request.RegrasTreino.Trim(),
            command.Request.BaseConhecimento.Trim(),
            command.Request.ExemplosBoasRespostas.Trim(),
            true,
            now,
            NormalizeOptional(command.Request.Observacao));

        await _adminIaRepository.SalvarEnsinoPublicadoAsync(command.TenantId, teaching, cancellationToken);
        _adminRepository.AdicionarAuditoria(new Auditoria
        {
            TenantId = command.TenantId,
            UsuarioId = command.AdminUsuarioId,
            Acao = "ia.ensino.publicado",
            Entidade = "OperacaoIA",
            Ip = Truncate(command.Ip, 64),
            UserAgent = Truncate(command.UserAgent, 512),
            MetadadosJson = JsonSerializer.Serialize(new { Versao = version, command.Request.Observacao }, JsonOptions)
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AdminIaEnsinoResponse>.Success(AdminIaMapper.Map(teaching));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
