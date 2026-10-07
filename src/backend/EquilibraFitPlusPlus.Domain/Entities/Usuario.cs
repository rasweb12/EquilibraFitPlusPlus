using EquilibraFitPlusPlus.Domain.Common;
using EquilibraFitPlusPlus.Domain.Enums;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Represents the business profile linked to Supabase Auth.
/// </summary>
public sealed class Usuario : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Supabase auth.users.id; legacy property name retained for internal compatibility.</summary>
    public Guid IdentityUserId { get; set; }

    /// <summary>Full user name.</summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>Normalized user email for business queries.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Current account status.</summary>
    public UsuarioStatus Status { get; set; } = UsuarioStatus.Ativo;

    /// <summary>Main application role.</summary>
    public UsuarioRole Role { get; set; } = UsuarioRole.Usuario;

    /// <summary>Last successful login date.</summary>
    public DateTimeOffset? UltimoLoginEm { get; set; }

    /// <summary>Rejects JWTs issued before a global session revocation, including unseen sessions.</summary>
    public DateTimeOffset? SessoesRevogadasAntesDe { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }

    /// <summary>User tenant.</summary>
    public Tenant? Tenant { get; set; }

    /// <summary>User health profile.</summary>
    public PerfilSaude? PerfilSaude { get; set; }

    /// <summary>User refresh tokens.</summary>
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
