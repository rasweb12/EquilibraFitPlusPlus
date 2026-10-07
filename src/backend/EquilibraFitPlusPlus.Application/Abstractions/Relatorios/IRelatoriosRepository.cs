using EquilibraFitPlusPlus.Domain.Entities;

namespace EquilibraFitPlusPlus.Application.Abstractions.Relatorios;

/// <summary>
/// Provides report source data.
/// </summary>
public interface IRelatoriosRepository
{
    /// <summary>Gets user report source data.</summary>
    Task<RelatorioUsuarioSourceData> ObterRelatorioUsuarioAsync(Guid tenantId, Guid usuarioId, DateOnly inicio, DateOnly fim, CancellationToken cancellationToken);

    /// <summary>Gets administrative report source data.</summary>
    Task<RelatorioAdminSourceData> ObterRelatorioAdminAsync(Guid tenantId, DateOnly inicio, DateOnly fim, CancellationToken cancellationToken);

    /// <summary>Lists support-safe engaged users.</summary>
    Task<IReadOnlyCollection<UsuarioEngajadoSourceData>> ListarUsuariosEngajadosAsync(Guid tenantId, DateOnly inicio, DateOnly fim, int take, CancellationToken cancellationToken);
}

/// <summary>
/// Source data used to build a user report.
/// </summary>
public sealed record RelatorioUsuarioSourceData(
    IReadOnlyCollection<RegistroAlimentar> RegistrosAlimentares,
    IReadOnlyCollection<TreinoUsuario> TreinosAtivos,
    IReadOnlyCollection<RegistroEvolucao> Evolucoes);

/// <summary>
/// Source data used to build an administrative report.
/// </summary>
public sealed record RelatorioAdminSourceData(
    int UsuariosAtivos,
    int NovosUsuarios,
    int RefeicoesRegistradas,
    int PlanosGerados,
    int TreinosAtivos,
    int SessoesCoach,
    int ReconhecimentosImagem,
    decimal Receita);

/// <summary>
/// Source data used to calculate an engaged user ranking.
/// </summary>
public sealed record UsuarioEngajadoSourceData(Guid UsuarioId, string Nome, int RegistrosAlimentares, int TreinosAtivos);
