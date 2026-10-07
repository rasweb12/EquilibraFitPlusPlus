namespace EquilibraFitPlusPlus.Contracts.Onboarding;

/// <summary>
/// User health questionnaire response.
/// </summary>
public sealed record QuestionarioResponse(
    Guid Id,
    DateOnly DataNascimento,
    int Idade,
    string SexoBiologico,
    decimal AlturaCm,
    decimal PesoAtualKg,
    decimal Imc,
    string ClassificacaoImc,
    string Objetivo,
    string NivelAtividade,
    byte DiasTreinoSemana,
    IReadOnlyCollection<string> Preferencias,
    IReadOnlyCollection<string> Restricoes,
    IReadOnlyCollection<string> Observacoes,
    string Mensagem);
