using EquilibraFitPlusPlus.Domain.Common;
using EquilibraFitPlusPlus.Domain.Enums;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Stores a hashed refresh token and its lifecycle.
/// </summary>
public sealed class RefreshToken : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Business user identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>SHA-256 hash of the refresh token.</summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>UTC expiration date.</summary>
    public DateTimeOffset ExpiraEm { get; set; }

    /// <summary>UTC revocation date.</summary>
    public DateTimeOffset? RevogadoEm { get; set; }

    /// <summary>Hash of the replacement token when rotation occurs.</summary>
    public string? SubstituidoPorTokenHash { get; set; }

    /// <summary>IP address used to create the token.</summary>
    public string? CriadoPorIp { get; set; }

    /// <summary>IP address used to revoke the token.</summary>
    public string? RevogadoPorIp { get; set; }

    /// <summary>Indicates whether the refresh token can still be used.</summary>
    public bool Ativo => RevogadoEm is null && DateTimeOffset.UtcNow < ExpiraEm;

    /// <summary>Owner user.</summary>
    public Usuario? Usuario { get; set; }
}

/// <summary>
/// Records the user's consent to a specific policy version.
/// </summary>
public sealed class ConsentimentoUsuario : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Consent type.</summary>
    public TipoConsentimento Tipo { get; set; }

    /// <summary>Accepted policy version.</summary>
    public string Versao { get; set; } = string.Empty;

    /// <summary>UTC acceptance date.</summary>
    public DateTimeOffset AceitoEm { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Consent source channel.</summary>
    public string Origem { get; set; } = "Mobile";

    /// <summary>Consent owner.</summary>
    public Usuario? Usuario { get; set; }
}
