using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Features.Admin.Mappings;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAdminIaEnsinoVersoes;

/// <summary>
/// Handles AI teaching version reads.
/// </summary>
public sealed class ListarAdminIaEnsinoVersoesQueryHandler : IRequestHandler<ListarAdminIaEnsinoVersoesQuery, Result<IReadOnlyCollection<AdminIaEnsinoVersaoResponse>>>
{
    private readonly IAdminIaRepository _adminIaRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ListarAdminIaEnsinoVersoesQueryHandler(IAdminIaRepository adminIaRepository)
    {
        _adminIaRepository = adminIaRepository;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyCollection<AdminIaEnsinoVersaoResponse>>> Handle(ListarAdminIaEnsinoVersoesQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<AdminIaEnsinoVersaoSourceData> versions = await _adminIaRepository.ListarVersoesEnsinoAsync(request.TenantId, 30, cancellationToken);
        return Result<IReadOnlyCollection<AdminIaEnsinoVersaoResponse>>.Success(versions.Select(AdminIaMapper.Map).ToArray());
    }
}
