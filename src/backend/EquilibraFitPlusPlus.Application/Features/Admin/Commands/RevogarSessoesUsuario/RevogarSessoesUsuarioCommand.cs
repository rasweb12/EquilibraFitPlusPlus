using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.RevogarSessoesUsuario;

/// <summary>
/// Command used by administrators to revoke active user sessions.
/// </summary>
public sealed record RevogarSessoesUsuarioCommand(
    Guid TenantId,
    Guid AdminUsuarioId,
    Guid UsuarioId,
    string? Ip,
    string? UserAgent) : IRequest<Result<RevogarSessoesUsuarioResponse>>;
