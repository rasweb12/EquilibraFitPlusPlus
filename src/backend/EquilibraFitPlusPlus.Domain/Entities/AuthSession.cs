using EquilibraFitPlusPlus.Domain.Common;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>API session lifecycle keyed by Supabase session_id, including refresh requests.</summary>
public sealed class AuthSession : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UsuarioId { get; set; }
    public Guid SessionId { get; set; }
    public DateTimeOffset? RevogadoEm { get; set; }
    public Usuario? Usuario { get; set; }
}
