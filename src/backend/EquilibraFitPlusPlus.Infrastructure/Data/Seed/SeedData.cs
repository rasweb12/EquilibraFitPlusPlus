using EquilibraFitPlusPlus.Domain.Enums;

namespace EquilibraFitPlusPlus.Infrastructure.Data.Seed;

/// <summary>
/// Deterministic seed identifiers for required records.
/// </summary>
public static class SeedData
{
    /// <summary>Default tenant identifier.</summary>
    public static readonly Guid DefaultTenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Role identifiers.</summary>
    public static readonly IReadOnlyDictionary<UsuarioRole, Guid> RoleIds = new Dictionary<UsuarioRole, Guid>
    {
        [UsuarioRole.Usuario] = Guid.Parse("22222222-2222-2222-2222-222222222201"),
        [UsuarioRole.Administrador] = Guid.Parse("22222222-2222-2222-2222-222222222202"),
        [UsuarioRole.Suporte] = Guid.Parse("22222222-2222-2222-2222-222222222203"),
        [UsuarioRole.Conteudo] = Guid.Parse("22222222-2222-2222-2222-222222222204"),
        [UsuarioRole.OperacaoIa] = Guid.Parse("22222222-2222-2222-2222-222222222205"),
        [UsuarioRole.Financeiro] = Guid.Parse("22222222-2222-2222-2222-222222222206")
    };

    /// <summary>Deterministic Identity role concurrency stamps.</summary>
    public static readonly IReadOnlyDictionary<UsuarioRole, string> RoleConcurrencyStamps = new Dictionary<UsuarioRole, string>
    {
        [UsuarioRole.Usuario] = "seed-role-usuario-v1",
        [UsuarioRole.Administrador] = "seed-role-administrador-v1",
        [UsuarioRole.Suporte] = "seed-role-suporte-v1",
        [UsuarioRole.Conteudo] = "seed-role-conteudo-v1",
        [UsuarioRole.OperacaoIa] = "seed-role-operacaoia-v1",
        [UsuarioRole.Financeiro] = "seed-role-financeiro-v1"
    };
}
