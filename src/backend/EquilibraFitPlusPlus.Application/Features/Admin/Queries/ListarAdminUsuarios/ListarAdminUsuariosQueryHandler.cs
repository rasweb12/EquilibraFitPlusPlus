using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Features.Admin.Mappings;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAdminUsuarios;

/// <summary>
/// Handles administrative user listing.
/// </summary>
public sealed class ListarAdminUsuariosQueryHandler : IRequestHandler<ListarAdminUsuariosQuery, Result<PagedResult<AdminUsuarioResumoResponse>>>
{
    private readonly IAdminRepository _adminRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ListarAdminUsuariosQueryHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<AdminUsuarioResumoResponse>>> Handle(ListarAdminUsuariosQuery request, CancellationToken cancellationToken)
    {
        PagedResult<Usuario> usuarios = await _adminRepository.ListarUsuariosAsync(request.TenantId, request.Termo, request.Page, request.PageSize, cancellationToken);
        var response = new PagedResult<AdminUsuarioResumoResponse>(
            usuarios.Items.Select(AdminMapper.Map).ToArray(),
            usuarios.Page,
            usuarios.PageSize,
            usuarios.TotalItems);

        return Result<PagedResult<AdminUsuarioResumoResponse>>.Success(response);
    }
}
