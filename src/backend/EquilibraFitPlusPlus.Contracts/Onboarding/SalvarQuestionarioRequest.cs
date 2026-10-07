namespace EquilibraFitPlusPlus.Contracts.Onboarding;

/// <summary>
/// Request used to create or update the initial user health questionnaire.
/// </summary>
public sealed record SalvarQuestionarioRequest(
    DateOnly DataNascimento,
    string SexoBiologico,
    decimal AlturaCm,
    decimal PesoAtualKg,
    string Objetivo,
    string NivelAtividade,
    byte DiasTreinoSemana,
    IReadOnlyCollection<string>? Preferencias,
    IReadOnlyCollection<string>? Restricoes,
    IReadOnlyCollection<string>? Observacoes);
