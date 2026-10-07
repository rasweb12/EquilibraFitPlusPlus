namespace EquilibraFitPlusPlus.Application.Abstractions.Admin;

/// <summary>
/// Provides external AI service health information.
/// </summary>
public interface IAdminIaHealthClient
{
    /// <summary>
    /// Gets external AI service status.
    /// </summary>
    Task<AdminIaServicoStatusSourceData> ObterStatusAsync(CancellationToken cancellationToken);
}

/// <summary>
/// External AI service status source data.
/// </summary>
public sealed record AdminIaServicoStatusSourceData(
    bool Configurada,
    bool Online,
    int? LatenciaMs,
    string? BaseUrl,
    string Modelo,
    string Mensagem);
