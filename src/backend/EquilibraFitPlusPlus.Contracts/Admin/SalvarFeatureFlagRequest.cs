namespace EquilibraFitPlusPlus.Contracts.Admin;

/// <summary>
/// Request used to create or update a feature flag.
/// </summary>
public sealed record SalvarFeatureFlagRequest(string Chave, bool Habilitada, string? ConfiguracaoJson);
