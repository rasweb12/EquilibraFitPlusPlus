using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.SalvarFeatureFlag;

/// <summary>
/// Command used to create or update a feature flag.
/// </summary>
public sealed record SalvarFeatureFlagCommand(
    Guid TenantId,
    Guid AdminUsuarioId,
    string? Ip,
    string? UserAgent,
    SalvarFeatureFlagRequest Request) : IRequest<Result<FeatureFlagResponse>>;
