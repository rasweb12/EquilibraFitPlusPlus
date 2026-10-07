namespace EquilibraFitPlusPlus.Contracts.Evolucao;

/// <summary>
/// Request used to save a body weight progress log.
/// </summary>
public sealed record SalvarPesoRequest(
    DateOnly? Data,
    decimal PesoKg,
    decimal? PercentualGordura,
    decimal? PercentualMassaMagra,
    string? Observacao);

/// <summary>
/// Body evolution log response.
/// </summary>
public sealed record RegistroEvolucaoResponse(
    Guid Id,
    DateOnly Data,
    decimal PesoKg,
    decimal? PercentualGordura,
    decimal? PercentualMassaMagra,
    string? Observacao,
    string Mensagem);
