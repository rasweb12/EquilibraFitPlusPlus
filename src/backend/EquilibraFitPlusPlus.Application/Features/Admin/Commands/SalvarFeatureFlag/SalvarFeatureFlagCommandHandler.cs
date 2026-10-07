using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Features.Admin.Mappings;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.SalvarFeatureFlag;

/// <summary>
/// Handles feature flag creation and updates.
/// </summary>
public sealed class SalvarFeatureFlagCommandHandler : IRequestHandler<SalvarFeatureFlagCommand, Result<FeatureFlagResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public SalvarFeatureFlagCommandHandler(IAdminRepository adminRepository, IUnitOfWork unitOfWork)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<FeatureFlagResponse>> Handle(SalvarFeatureFlagCommand command, CancellationToken cancellationToken)
    {
        string chave = command.Request.Chave.Trim().ToLowerInvariant();
        FeatureFlag? featureFlag = await _adminRepository.ObterFeatureFlagAsync(command.TenantId, chave, cancellationToken);
        bool created = featureFlag is null;

        featureFlag ??= new FeatureFlag
        {
            TenantId = command.TenantId,
            Chave = chave
        };

        featureFlag.Habilitada = command.Request.Habilitada;
        featureFlag.ConfiguracaoJson = string.IsNullOrWhiteSpace(command.Request.ConfiguracaoJson)
            ? null
            : command.Request.ConfiguracaoJson.Trim();

        if (created)
        {
            _adminRepository.AdicionarFeatureFlag(featureFlag);
        }

        _adminRepository.AdicionarAuditoria(new Auditoria
        {
            TenantId = command.TenantId,
            UsuarioId = command.AdminUsuarioId,
            Acao = created ? "feature_flag.criada" : "feature_flag.atualizada",
            Entidade = nameof(FeatureFlag),
            EntidadeId = featureFlag.Id,
            Ip = Truncate(command.Ip, 64),
            UserAgent = Truncate(command.UserAgent, 512),
            MetadadosJson = JsonSerializer.Serialize(new
            {
                featureFlag.Chave,
                featureFlag.Habilitada,
                Acao = created ? "Criacao" : "Atualizacao"
            }, JsonOptions)
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<FeatureFlagResponse>.Success(AdminMapper.Map(featureFlag));
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
