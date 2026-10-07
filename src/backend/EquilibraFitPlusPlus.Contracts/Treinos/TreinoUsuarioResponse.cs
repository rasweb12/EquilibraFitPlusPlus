namespace EquilibraFitPlusPlus.Contracts.Treinos;

/// <summary>
/// User workout response.
/// </summary>
public sealed record TreinoUsuarioResponse(
    Guid Id,
    string Nome,
    string Objetivo,
    byte FrequenciaSemanal,
    bool Ativo,
    IReadOnlyCollection<TreinoExercicioResponse> Exercicios,
    string Mensagem,
    int Versao,
    Guid? TreinoAnteriorId,
    DateOnly DataInicio,
    DateOnly? DataFim,
    int DuracaoSemanas,
    int SemanaAtual,
    string Fase);

/// <summary>
/// Exercise prescription response.
/// </summary>
public sealed record TreinoExercicioResponse(
    Guid Id,
    Guid ExercicioId,
    string Nome,
    string GrupoMuscular,
    string Nivel,
    string? Equipamento,
    string Instrucao,
    int DiaTreino,
    bool ConcluidoHoje,
    int Ordem,
    string? Observacao,
    int Series,
    string Repeticoes,
    int DescansoSegundos,
    decimal? CargaAlvoKg,
    byte? RpeAlvo,
    int? RepeticoesMin,
    int? RepeticoesMax,
    string? ProgressaoMotivo);

/// <summary>
/// Request used to mark a workout exercise as completed or pending.
/// </summary>
public sealed record ConcluirTreinoExercicioRequest(bool Concluido, DateOnly? Data);

/// <summary>
/// Response returned after updating a workout exercise completion status.
/// </summary>
public sealed record TreinoExercicioConclusaoResponse(
    Guid TreinoUsuarioId,
    Guid TreinoExercicioId,
    DateOnly Data,
    bool Concluido,
    DateTimeOffset? ConcluidoEm,
    string Mensagem);

/// <summary>
/// Response for a real workout execution session.
/// </summary>
public sealed record TreinoSessaoResponse(
    Guid Id,
    Guid TreinoUsuarioId,
    DateOnly Data,
    byte DiaTreino,
    DateTimeOffset IniciadoEm,
    DateTimeOffset? FinalizadoEm,
    string? Observacao,
    string Resumo,
    IReadOnlyCollection<TreinoSerieRealizadaResponse> Series);

/// <summary>
/// Response for one performed set.
/// </summary>
public sealed record TreinoSerieRealizadaResponse(
    Guid Id,
    Guid TreinoExercicioId,
    int NumeroSerie,
    decimal? CargaKg,
    int RepeticoesRealizadas,
    byte Rpe,
    string? Observacao,
    bool DorDesconforto,
    string? DorDescricao);

/// <summary>
/// Exercise history response.
/// </summary>
public sealed record HistoricoExercicioResponse(
    Guid ExercicioId,
    string Nome,
    string GrupoMuscular,
    decimal? CargaAnteriorKg,
    decimal? CargaMaximaKg,
    decimal VolumeTotalKg,
    decimal EvolucaoCargaKg,
    IReadOnlyCollection<HistoricoExercicioSessaoResponse> UltimasSessoes);

/// <summary>
/// Exercise history session item.
/// </summary>
public sealed record HistoricoExercicioSessaoResponse(
    DateOnly Data,
    decimal? MaiorCargaKg,
    int RepeticoesTotais,
    decimal VolumeKg,
    byte? RpeMedio);

/// <summary>
/// Workout evolution proposal response.
/// </summary>
public sealed record TreinoEvolucaoPropostaResponse(
    Guid Id,
    Guid TreinoUsuarioId,
    int VersaoAtual,
    int VersaoProposta,
    string Status,
    string MudancasResumo,
    CriarTreinoRequest PlanoProposto,
    IReadOnlyCollection<TreinoEvolucaoMudancaResponse> Mudancas,
    string Mensagem);

/// <summary>
/// Detailed comparison item for a workout evolution proposal.
/// </summary>
public sealed record TreinoEvolucaoMudancaResponse(
    string ExercicioNome,
    int SeriesAtual,
    int SeriesProposta,
    string RepeticoesAtual,
    string RepeticoesProposta,
    decimal? CargaAtualKg,
    decimal? CargaPropostaKg,
    int DescansoAtualSegundos,
    int DescansoPropostoSegundos,
    string Motivo);

/// <summary>
/// Exercise progression suggestion response.
/// </summary>
public sealed record TreinoProgressaoSugestaoResponse(
    Guid Id,
    Guid TreinoUsuarioId,
    Guid TreinoExercicioId,
    string ExercicioNome,
    decimal? CargaAtualKg,
    decimal? CargaSugeridaKg,
    byte? RpeAlvo,
    int? RepeticoesMin,
    int? RepeticoesMax,
    string Motivo,
    string Status,
    string Mensagem);
