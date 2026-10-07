using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.RevogarSessoesUsuario;

/// <summary>
/// Handles administrative user session revocation.
/// </summary>
public sealed class RevogarSessoesUsuarioCommandHandler
    : IRequestHandler<RevogarSessoesUsuarioCommand, Result<RevogarSessoesUsuarioResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Initializes the handler.</summary>
    public RevogarSessoesUsuarioCommandHandler(IAdminRepository adminRepository, IUnitOfWork unitOfWork)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<RevogarSessoesUsuarioResponse>> Handle(RevogarSessoesUsuarioCommand command, CancellationToken cancellationToken)
    {
        if (command.UsuarioId == command.AdminUsuarioId)
        {
            return Result<RevogarSessoesUsuarioResponse>.Failure(new Error(
                "admin.nao_revogar_propria_sessao",
                "Use o botão Sair para encerrar sua própria sessão administrativa."));
        }

        Usuario? usuario = await _adminRepository.ObterUsuarioAsync(command.TenantId, command.UsuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result<RevogarSessoesUsuarioResponse>.Failure(new Error(
                "admin.usuario_nao_encontrado",
                "Não encontramos esse usuário para revogar sessões."));
        }

        int revoked = await _adminRepository.RevogarSessoesUsuarioAsync(command.TenantId, command.UsuarioId, command.Ip, cancellationToken);

        _adminRepository.AdicionarAuditoria(new Auditoria
        {
            TenantId = command.TenantId,
            UsuarioId = command.AdminUsuarioId,
            Acao = "usuario.sessoes.revogar",
            Entidade = nameof(Usuario),
            EntidadeId = usuario.Id,
            Ip = Truncate(command.Ip, 64),
            UserAgent = Truncate(command.UserAgent, 512),
            MetadadosJson = JsonSerializer.Serialize(new
            {
                usuario.Email,
                SessoesRevogadas = revoked
            }, JsonOptions)
        });

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        string message = revoked == 0
            ? "O usuário não tinha sessões renováveis ativas."
            : $"{revoked} sessão(ões) renovável(is) encerrada(s).";

        return Result<RevogarSessoesUsuarioResponse>.Success(new RevogarSessoesUsuarioResponse(usuario.Id, revoked, message));
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
