using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Features.Admin.Mappings;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAuditorias;

/// <summary>
/// Handles administrative audit listing.
/// </summary>
public sealed class ListarAuditoriasQueryHandler : IRequestHandler<ListarAuditoriasQuery, Result<PagedResult<AuditoriaResponse>>>
{
    private readonly IAdminRepository _adminRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ListarAuditoriasQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<AuditoriaResponse>>> Handle(ListarAuditoriasQuery request, CancellationToken cancellationToken)
    {
        PagedResult<Auditoria> auditorias = await _adminRepository.ListarAuditoriasAsync(request.TenantId, request.Entidade, request.Page, request.PageSize, cancellationToken);
        var response = new PagedResult<AuditoriaResponse>(
            auditorias.Items.Select(AdminMapper.Map).ToArray(),
            auditorias.Page,
            auditorias.PageSize,
            auditorias.TotalItems);

        return Result<PagedResult<AuditoriaResponse>>.Success(response);
    }
}
