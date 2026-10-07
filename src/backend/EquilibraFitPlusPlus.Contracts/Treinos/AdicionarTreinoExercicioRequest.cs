using System;
using System.Collections.Generic;
using System.Text;

namespace EquilibraFitPlusPlus.Contracts.Treinos;

public sealed record AdicionarTreinoExercicioRequest(
    Guid? ExercicioId,
    string? Nome,
    string? GrupoMuscular,
    string? Nivel,
    string? Equipamento,
    string? Instrucao,
    byte DiaTreino,
    int Ordem,
    int Series,
    string Repeticoes,
    int DescansoSegundos,
    decimal? CargaAlvoKg,
    byte? RpeAlvo,
    int? RepeticoesMin,
    int? RepeticoesMax,
    string? Observacao);
