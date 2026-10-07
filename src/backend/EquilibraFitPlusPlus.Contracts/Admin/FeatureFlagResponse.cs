namespace EquilibraFitPlusPlus.Contracts.Admin;

/// <summary>
/// Feature flag response.
/// </summary>
public sealed record FeatureFlagResponse(Guid Id, string Chave, bool Habilitada, string? ConfiguracaoJson, DateTimeOffset CriadoEm, DateTimeOffset? AtualizadoEm);
