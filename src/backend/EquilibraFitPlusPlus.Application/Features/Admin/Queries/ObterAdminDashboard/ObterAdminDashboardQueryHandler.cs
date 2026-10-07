using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Features.Admin.Mappings;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ObterAdminDashboard;

/// <summary>
/// Handles administrative dashboard reads.
/// </summary>
public sealed class ObterAdminDashboardQueryHandler : IRequestHandler<ObterAdminDashboardQuery, Result<AdminDashboardResponse>>
{
    private readonly IAdminRepository _adminRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ObterAdminDashboardQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    /// <inheritdoc />
    public async Task<Result<AdminDashboardResponse>> Handle(ObterAdminDashboardQuery request, CancellationToken cancellationToken)
    {
        AdminDashboardSourceData source = await _adminRepository.ObterDashboardAsync(request.TenantId, request.DataReferencia, cancellationToken);
        return Result<AdminDashboardResponse>.Success(AdminMapper.Map(source));
    }
}
