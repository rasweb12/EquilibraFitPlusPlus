namespace EquilibraFitPlusPlus.Contracts.Treinos;

/// <summary>
/// Exercise catalog response.
/// </summary>
public sealed record ExercicioResponse(
    Guid Id,
    string Nome,
    string GrupoMuscular,
    string Nivel,
    string? Equipamento,
    string Instrucao);
