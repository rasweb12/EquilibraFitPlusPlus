using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Features.Admin.Mappings;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.RestaurarAdminIaEnsino;

/// <summary>
/// Handles AI teaching restoration.
/// </summary>
public sealed class RestaurarAdminIaEnsinoCommandHandler : IRequestHandler<RestaurarAdminIaEnsinoCommand, Result<AdminIaEnsinoResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAdminIaRepository _adminIaRepository;
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public RestaurarAdminIaEnsinoCommandHandler(IAdminIaRepository adminIaRepository, IAdminRepository adminRepository, IUnitOfWork unitOfWork)
    {
        _adminIaRepository = adminIaRepository;
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<AdminIaEnsinoResponse>> Handle(RestaurarAdminIaEnsinoCommand command, CancellationToken cancellationToken)
    {
        AdminIaEnsinoSourceData? previous = await _adminIaRepository.ObterEnsinoPorVersaoAsync(command.TenantId, command.Request.Versao, cancellationToken);
        if (previous is null)
        {
            return Result<AdminIaEnsinoResponse>.Failure(new Error("ia.ensino_versao_nao_encontrada", "Não encontramos essa versão de ensino da IA."));
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string newVersion = "v" + now.ToString("yyyyMMddHHmmss");
        AdminIaEnsinoSourceData restored = previous with
        {
            Versao = newVersion,
            Publicada = true,
            PublicadaEm = now,
            Observacao = string.IsNullOrWhiteSpace(command.Request.Observacao)
                ? "Restaurada a partir da versao " + command.Request.Versao
                : command.Request.Observacao.Trim()
        };

        await _adminIaRepository.SalvarEnsinoPublicadoAsync(command.TenantId, restored, cancellationToken);
        _adminRepository.AdicionarAuditoria(new Auditoria
        {
            TenantId = command.TenantId,
            UsuarioId = command.AdminUsuarioId,
            Acao = "ia.ensino.restaurado",
            Entidade = "OperacaoIA",
            Ip = Truncate(command.Ip, 64),
            UserAgent = Truncate(command.UserAgent, 512),
            MetadadosJson = JsonSerializer.Serialize(new { VersaoOrigem = command.Request.Versao, VersaoNova = newVersion }, JsonOptions)
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<AdminIaEnsinoResponse>.Success(AdminIaMapper.Map(restored));
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
