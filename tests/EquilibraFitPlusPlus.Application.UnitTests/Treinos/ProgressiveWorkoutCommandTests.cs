using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Treinos;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AplicarPropostaEvolucaoTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AdicionarTreinoExercicio;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AtualizarTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.AtualizarTreinoExercicio;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.DecidirProgressaoTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.GerarPropostaEvolucaoTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.RegistrarSessaoTreino;
using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.SubstituirTreinoExercicio;
using EquilibraFitPlusPlus.Application.Features.Treinos.Services;
using EquilibraFitPlusPlus.Contracts.Treinos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Pagination;

namespace EquilibraFitPlusPlus.Application.UnitTests.Treinos;

/// <summary>
/// Tests progressive workout commands and progression rules.
/// </summary>
public sealed class ProgressiveWorkoutCommandTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    /// <summary>
    /// Ensures workout metadata can be edited without creating a new version.
    /// </summary>
    [Fact]
    public async Task AtualizarTreino_ShouldUpdateMetadata()
    {
        var repository = new FakeTreinoRepository(CreateWorkout());
        var handler = new AtualizarTreinoCommandHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(
            new AtualizarTreinoCommand(
                TenantId,
                UserId,
                repository.Workout.Id,
                new AtualizarTreinoRequest("Treino base ajustado", "Força", 4, new DateOnly(2026, 8, 8), 8, "Fase 2")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Treino base ajustado", repository.Workout.Nome);
        Assert.Equal(4, repository.Workout.FrequenciaSemanal);
        Assert.Equal("Fase 2", repository.Workout.Fase);
    }

    /// <summary>
    /// Ensures prescribed exercise edits persist day and structured progression fields.
    /// </summary>
    [Fact]
    public async Task AtualizarExercicio_ShouldPersistDayAndProgressionFields()
    {
        TreinoUsuario workout = CreateWorkout();
        var repository = new FakeTreinoRepository(workout);
        var handler = new AtualizarTreinoExercicioCommandHandler(repository, new FakeUnitOfWork());
        TreinoExercicio exercise = workout.Exercicios.Single();

        var result = await handler.Handle(
            new AtualizarTreinoExercicioCommand(
                TenantId,
                UserId,
                workout.Id,
                exercise.Id,
                new AtualizarTreinoExercicioRequest(2, 3, 4, "8-10", 90, "Barra", "Controle total", 72.5m, 8, 8, 10, "Topo da faixa por duas sessões.")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, exercise.DiaTreino);
        Assert.Equal(4, exercise.Series);
        Assert.Equal(72.5m, exercise.CargaAlvoKg);
        Assert.Equal((byte?)8, exercise.RpeAlvo);
        Assert.Equal(8, exercise.RepeticoesMin);
        Assert.Equal(10, exercise.RepeticoesMax);
        Assert.Equal("Topo da faixa por duas sessões.", exercise.ProgressaoMotivo);
    }

    /// <summary>
    /// Ensures a move to an occupied position appends the exercise to the target day.
    /// </summary>
    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public async Task AtualizarExercicio_ShouldAppendWhenTargetOrderIsOccupied(
        int sourceDay,
        int targetDay)
    {
        TreinoUsuario workout = CreateWorkout();
        var repository = new FakeTreinoRepository(workout);
        TreinoExercicio exercise = workout.Exercicios.Single();
        exercise.DiaTreino = (byte)sourceDay;
        workout.Exercicios.Add(CreatePrescribedExercise(workout, (byte)targetDay, 1, "Remada"));
        var handler = new AtualizarTreinoExercicioCommandHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(
            new AtualizarTreinoExercicioCommand(
                TenantId,
                UserId,
                workout.Id,
                exercise.Id,
                CreateExerciseUpdateRequest((byte)targetDay, 1)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal((byte)targetDay, exercise.DiaTreino);
        Assert.Equal(2, exercise.Ordem);
    }

    /// <summary>
    /// Ensures the first exercise moved to an empty day starts at order one.
    /// </summary>
    [Fact]
    public async Task AtualizarExercicio_ShouldUseOrderOneWhenTargetDayIsEmpty()
    {
        TreinoUsuario workout = CreateWorkout();
        var repository = new FakeTreinoRepository(workout);
        TreinoExercicio exercise = workout.Exercicios.Single();
        var handler = new AtualizarTreinoExercicioCommandHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(
            new AtualizarTreinoExercicioCommand(
                TenantId,
                UserId,
                workout.Id,
                exercise.Id,
                CreateExerciseUpdateRequest(3, 7)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, exercise.DiaTreino);
        Assert.Equal(1, exercise.Ordem);
    }

    /// <summary>
    /// Ensures editing within the same day cannot reuse another active position.
    /// </summary>
    [Fact]
    public async Task AtualizarExercicio_ShouldRejectDuplicateOrderWithinSameDay()
    {
        TreinoUsuario workout = CreateWorkout();
        var repository = new FakeTreinoRepository(workout);
        TreinoExercicio exercise = workout.Exercicios.Single();
        workout.Exercicios.Add(CreatePrescribedExercise(workout, 1, 2, "Crucifixo"));
        var handler = new AtualizarTreinoExercicioCommandHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(
            new AtualizarTreinoExercicioCommand(
                TenantId,
                UserId,
                workout.Id,
                exercise.Id,
                CreateExerciseUpdateRequest(1, 2)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == "treinos.ordem_duplicada");
        Assert.Equal(1, exercise.Ordem);
    }

    /// <summary>
    /// Ensures a soft-deleted exercise does not reserve its former position.
    /// </summary>
    [Fact]
    public async Task AtualizarExercicio_ShouldIgnoreSoftDeletedTargetPosition()
    {
        TreinoUsuario workout = CreateWorkout();
        var repository = new FakeTreinoRepository(workout);
        TreinoExercicio exercise = workout.Exercicios.Single();
        TreinoExercicio excluded = CreatePrescribedExercise(workout, 2, 1, "Remada excluída");
        excluded.ExcluidoEm = DateTimeOffset.UtcNow;
        workout.Exercicios.Add(excluded);
        var handler = new AtualizarTreinoExercicioCommandHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(
            new AtualizarTreinoExercicioCommand(
                TenantId,
                UserId,
                workout.Id,
                exercise.Id,
                CreateExerciseUpdateRequest(2, 1)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, exercise.DiaTreino);
        Assert.Equal(1, exercise.Ordem);
    }

    /// <summary>
    /// Ensures persistence concurrency is returned as a domain-safe workout error.
    /// </summary>
    [Fact]
    public async Task AtualizarExercicio_ShouldHandlePersistenceConflict()
    {
        TreinoUsuario workout = CreateWorkout();
        var repository = new FakeTreinoRepository(workout);
        TreinoExercicio exercise = workout.Exercicios.Single();
        var handler = new AtualizarTreinoExercicioCommandHandler(
            repository,
            new ConflictingUnitOfWork());

        var result = await handler.Handle(
            new AtualizarTreinoExercicioCommand(
                TenantId,
                UserId,
                workout.Id,
                exercise.Id,
                CreateExerciseUpdateRequest(2, 1)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == "treinos.posicao_concorrente");
    }

    /// <summary>
    /// Ensures add conflicts do not leak provider-specific exceptions.
    /// </summary>
    [Fact]
    public async Task AdicionarExercicio_ShouldHandlePersistenceConflict()
    {
        TreinoUsuario workout = CreateWorkout();
        Exercicio catalogExercise = CreateExercise("Remada baixa", "Costas");
        var repository = new FakeTreinoRepository(workout);
        repository.Catalog[catalogExercise.Id] = catalogExercise;
        var handler = new AdicionarTreinoExercicioCommandHandler(
            repository,
            new ConflictingUnitOfWork());

        var result = await handler.Handle(
            new AdicionarTreinoExercicioCommand(
                TenantId,
                UserId,
                workout.Id,
                new AdicionarTreinoExercicioRequest(
                    catalogExercise.Id,
                    null,
                    null,
                    null,
                    null,
                    null,
                    1,
                    2,
                    3,
                    "10-12",
                    75,
                    null,
                    null,
                    null,
                    null,
                    null)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == "treinos.posicao_concorrente");
    }

    /// <summary>
    /// Ensures adding a prescribed exercise uses the repository insertion path.
    /// </summary>
    [Fact]
    public async Task AdicionarExercicio_ShouldUsePrescriptionInsertionPath()
    {
        TreinoUsuario workout = CreateWorkout();
        Exercicio catalogExercise = CreateExercise("Remada baixa", "Costas");
        var repository = new FakeTreinoRepository(workout);
        repository.Catalog[catalogExercise.Id] = catalogExercise;
        var handler = new AdicionarTreinoExercicioCommandHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(
            new AdicionarTreinoExercicioCommand(
                TenantId,
                UserId,
                workout.Id,
                new AdicionarTreinoExercicioRequest(
                    catalogExercise.Id,
                    null,
                    null,
                    null,
                    null,
                    null,
                    1,
                    2,
                    3,
                    "10-12",
                    75,
                    50m,
                    8,
                    10,
                    12,
                    "Manter controle.")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        TreinoExercicio added = Assert.Single(repository.AddedPrescriptions);
        Assert.Equal(workout.Id, added.TreinoUsuarioId);
        Assert.Equal(catalogExercise.Id, added.ExercicioId);
        Assert.Equal(2, added.Ordem);
    }

    /// <summary>
    /// Ensures exercise replacement preserves the prescription and changes only the catalog exercise.
    /// </summary>
    [Fact]
    public async Task SubstituirExercicio_ShouldReplaceExerciseWithSuggestedAlternative()
    {
        TreinoUsuario workout = CreateWorkout();
        Exercicio replacement = CreateExercise("Supino com halteres", "Peitoral");
        var repository = new FakeTreinoRepository(workout)
        {
            Substitutes = [replacement]
        };
        var handler = new SubstituirTreinoExercicioCommandHandler(repository, new FakeUnitOfWork());
        TreinoExercicio prescribed = workout.Exercicios.Single();

        var result = await handler.Handle(
            new SubstituirTreinoExercicioCommand(
                TenantId,
                UserId,
                workout.Id,
                prescribed.Id,
                new SubstituirTreinoExercicioRequest(null, null, null, null, null, null, true)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(replacement.Id, prescribed.ExercicioId);
        Assert.Equal("Peitoral", prescribed.Exercicio!.GrupoMuscular);
        Assert.Equal(1, prescribed.DiaTreino);
    }

    /// <summary>
    /// Ensures a workout execution records performed sets and daily checklist completion.
    /// </summary>
    [Fact]
    public async Task RegistrarSessao_ShouldCreateSessionSeriesAndCompletion()
    {
        TreinoUsuario workout = CreateWorkout();
        var repository = new FakeTreinoRepository(workout);
        var handler = new RegistrarSessaoTreinoCommandHandler(repository, new FakeUnitOfWork());
        Guid prescribedId = workout.Exercicios.Single().Id;

        var result = await handler.Handle(
            new RegistrarSessaoTreinoCommand(
                TenantId,
                UserId,
                workout.Id,
                new RegistrarSessaoTreinoRequest(
                    new DateOnly(2026, 8, 8),
                    1,
                    "Treino confortável.",
                    [new RegistrarSerieTreinoRequest(prescribedId, 1, 70m, 10, 7, "Boa técnica.", false, null)])),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        TreinoSessao session = Assert.Single(repository.AddedSessions);
        TreinoSerieRealizada set = Assert.Single(session.Series);
        Assert.Equal(70m, set.CargaKg);
        Assert.Equal(10, set.RepeticoesRealizadas);
        Assert.Single(repository.Conclusions);
        Assert.True(repository.Conclusions[0].Concluido);
    }

    /// <summary>
    /// Ensures offline retries with the same operation identifier do not duplicate sessions.
    /// </summary>
    [Fact]
    public async Task RegistrarSessao_ShouldBeIdempotentByOperationId()
    {
        TreinoUsuario workout = CreateWorkout();
        var repository = new FakeTreinoRepository(workout);
        var handler = new RegistrarSessaoTreinoCommandHandler(repository, new FakeUnitOfWork());
        Guid prescribedId = workout.Exercicios.Single().Id;
        Guid operationId = Guid.NewGuid();
        var request = new RegistrarSessaoTreinoRequest(
            new DateOnly(2026, 8, 8),
            1,
            "Retry offline.",
            [new RegistrarSerieTreinoRequest(prescribedId, 1, 70m, 10, 7, null, false, null)],
            operationId);

        var first = await handler.Handle(new RegistrarSessaoTreinoCommand(TenantId, UserId, workout.Id, request), CancellationToken.None);
        var second = await handler.Handle(new RegistrarSessaoTreinoCommand(TenantId, UserId, workout.Id, request), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Single(repository.AddedSessions);
        Assert.Equal(operationId, repository.AddedSessions[0].OperationId);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
        Assert.Single(repository.Conclusions);
    }

    /// <summary>
    /// Ensures progression suggestions are applied to structural fields, not free text notes.
    /// </summary>
    [Fact]
    public async Task DecidirProgressao_ShouldApplyStructuredProgression()
    {
        TreinoUsuario workout = CreateWorkout();
        TreinoExercicio exercise = workout.Exercicios.Single();
        var suggestion = new TreinoProgressaoSugestao
        {
            TenantId = TenantId,
            UsuarioId = UserId,
            TreinoUsuarioId = workout.Id,
            TreinoExercicioId = exercise.Id,
            TreinoExercicio = exercise,
            CargaAtualKg = 70m,
            CargaSugeridaKg = 72.5m,
            RpeAlvo = 8,
            RepeticoesMin = 8,
            RepeticoesMax = 10,
            Motivo = "Você atingiu o topo da faixa.",
            Status = "Pendente"
        };
        var repository = new FakeTreinoRepository(workout) { Suggestion = suggestion };
        var handler = new DecidirProgressaoTreinoCommandHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(
            new DecidirProgressaoTreinoCommand(TenantId, UserId, suggestion.Id, new DecidirProgressaoTreinoRequest(true)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Aplicada", suggestion.Status);
        Assert.Equal(72.5m, exercise.CargaAlvoKg);
        Assert.Equal((byte?)8, exercise.RpeAlvo);
        Assert.Equal("Você atingiu o topo da faixa.", exercise.ProgressaoMotivo);
        Assert.Null(exercise.Observacao);
    }

    /// <summary>
    /// Ensures keeping a progression does not mutate the prescribed exercise.
    /// </summary>
    [Fact]
    public async Task DecidirProgressao_ShouldKeepWithoutChangingExercise()
    {
        TreinoUsuario workout = CreateWorkout();
        TreinoExercicio exercise = workout.Exercicios.Single();
        var suggestion = new TreinoProgressaoSugestao
        {
            TenantId = TenantId,
            UsuarioId = UserId,
            TreinoUsuarioId = workout.Id,
            TreinoExercicioId = exercise.Id,
            TreinoExercicio = exercise,
            CargaAtualKg = 70m,
            CargaSugeridaKg = 72.5m,
            Motivo = "Sugestão conservadora.",
            Status = "Pendente"
        };
        var repository = new FakeTreinoRepository(workout) { Suggestion = suggestion };
        var handler = new DecidirProgressaoTreinoCommandHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(
            new DecidirProgressaoTreinoCommand(TenantId, UserId, suggestion.Id, new DecidirProgressaoTreinoRequest(false)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Mantida", suggestion.Status);
        Assert.Null(exercise.CargaAlvoKg);
        Assert.Null(exercise.ProgressaoMotivo);
    }

    /// <summary>
    /// Ensures evolution proposal creation does not replace the current workout automatically.
    /// </summary>
    [Fact]
    public async Task GerarProposta_ShouldCreateComparisonWithoutApplying()
    {
        TreinoUsuario workout = CreateWorkout();
        TreinoExercicio exercise = workout.Exercicios.Single();
        exercise.Series = 1;
        exercise.Repeticoes = "8-10";
        exercise.RepeticoesMin = 8;
        exercise.RepeticoesMax = 10;
        var repository = new FakeTreinoRepository(workout);
        repository.History.AddRange(CreateTopRangeHistory(exercise.Id, 70m, 10, 7, exercise.TreinoUsuarioId));
        var handler = new GerarPropostaEvolucaoTreinoCommandHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(
            new GerarPropostaEvolucaoTreinoCommand(TenantId, UserId, workout.Id, new GerarPropostaEvolucaoTreinoRequest(null)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(workout.Ativo);
        Assert.Single(repository.AddedProposals);
        Assert.NotEmpty(result.Value!.Mudancas);
        Assert.Equal(72.5m, result.Value.Mudancas.First().CargaPropostaKg);
    }

    /// <summary>
    /// Ensures applying a proposal finalizes the previous plan and creates a new active version.
    /// </summary>
    [Fact]
    public async Task AplicarProposta_ShouldPreservePreviousAndActivateNewVersion()
    {
        TreinoUsuario previous = CreateWorkout();
        Exercicio exercise = previous.Exercicios.Single().Exercicio!;
        var proposedPlan = new CriarTreinoRequest(
            "Treino base v2",
            previous.Objetivo,
            previous.FrequenciaSemanal,
            [new TreinoExercicioRequest(exercise.Id, null, null, null, exercise.Equipamento, null, 1, 1, 3, "8-10", 90, null, 72.5m, 8, 8, 10, "Progressão aprovada.")],
            new DateOnly(2026, 8, 8),
            6,
            "Fase 2");
        var proposal = new TreinoEvolucaoProposta
        {
            TenantId = TenantId,
            UsuarioId = UserId,
            TreinoUsuarioId = previous.Id,
            TreinoUsuario = previous,
            VersaoProposta = 2,
            PlanoJson = JsonSerializer.Serialize(proposedPlan, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            MudancasResumo = "Progressão segura.",
            Status = "Pendente"
        };
        var repository = new FakeTreinoRepository(previous) { Proposal = proposal };
        repository.Catalog[exercise.Id] = exercise;
        var handler = new AplicarPropostaEvolucaoTreinoCommandHandler(repository, new FakeUnitOfWork());

        var result = await handler.Handle(
            new AplicarPropostaEvolucaoTreinoCommand(TenantId, UserId, previous.Id, proposal.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(previous.Ativo);
        Assert.NotNull(previous.DataFim);
        TreinoUsuario next = Assert.Single(repository.AddedWorkouts);
        Assert.True(next.Ativo);
        Assert.Equal(previous.Id, next.TreinoAnteriorId);
        Assert.Equal(2, next.Versao);
        Assert.Equal(72.5m, next.Exercicios.Single().CargaAlvoKg);
    }

    /// <summary>
    /// Ensures progression is suggested only with enough safe evidence.
    /// </summary>
    [Fact]
    public void ProgressionAnalyzer_ShouldSuggestAfterTwoSafeTopRangeSessions()
    {
        TreinoExercicio exercise = CreateWorkout().Exercicios.Single();
        exercise.Series = 1;
        exercise.RepeticoesMin = 8;
        exercise.RepeticoesMax = 10;

        TreinoProgressionSuggestion? suggestion = TreinoProgressionAnalyzer.Suggest(
            exercise,
            CreateTopRangeHistory(exercise.Id, 70m, 10, 7, exercise.TreinoUsuarioId));

        Assert.NotNull(suggestion);
        Assert.Equal(72.5m, suggestion!.CargaSugeridaKg);
    }

    /// <summary>
    /// Ensures progression is blocked when history is insufficient.
    /// </summary>
    [Fact]
    public void ProgressionAnalyzer_ShouldNotSuggestWithInsufficientHistory()
    {
        TreinoExercicio exercise = CreateWorkout().Exercicios.Single();
        exercise.Series = 1;
        exercise.RepeticoesMin = 8;
        exercise.RepeticoesMax = 10;

        TreinoProgressionSuggestion? suggestion = TreinoProgressionAnalyzer.Suggest(
            exercise,
            CreateTopRangeHistory(exercise.Id, 70m, 10, 7, exercise.TreinoUsuarioId).Take(1).ToArray());

        Assert.Null(suggestion);
    }

    /// <summary>
    /// Ensures progression is blocked after pain or discomfort.
    /// </summary>
    [Fact]
    public void ProgressionAnalyzer_ShouldNotSuggestAfterPain()
    {
        TreinoExercicio exercise = CreateWorkout().Exercicios.Single();
        exercise.Series = 1;
        exercise.RepeticoesMin = 8;
        exercise.RepeticoesMax = 10;
        TreinoSerieRealizada[] history = CreateTopRangeHistory(exercise.Id, 70m, 10, 7, exercise.TreinoUsuarioId);
        history[0].DorDesconforto = true;

        TreinoProgressionSuggestion? suggestion = TreinoProgressionAnalyzer.Suggest(exercise, history);

        Assert.Null(suggestion);
    }

    /// <summary>
    /// Ensures progression is blocked when execution is below the prescribed range.
    /// </summary>
    [Fact]
    public void ProgressionAnalyzer_ShouldNotSuggestWhenBelowRange()
    {
        TreinoExercicio exercise = CreateWorkout().Exercicios.Single();
        exercise.Series = 1;
        exercise.RepeticoesMin = 8;
        exercise.RepeticoesMax = 10;
        TreinoSerieRealizada[] history = CreateTopRangeHistory(exercise.Id, 70m, 10, 7, exercise.TreinoUsuarioId);
        history[0].RepeticoesRealizadas = 7;

        TreinoProgressionSuggestion? suggestion = TreinoProgressionAnalyzer.Suggest(exercise, history);

        Assert.Null(suggestion);
    }

    /// <summary>
    /// Ensures progression is blocked when RPE is too high.
    /// </summary>
    [Fact]
    public void ProgressionAnalyzer_ShouldNotSuggestWhenRpeIsTooHigh()
    {
        TreinoExercicio exercise = CreateWorkout().Exercicios.Single();
        exercise.Series = 1;
        exercise.RepeticoesMin = 8;
        exercise.RepeticoesMax = 10;
        TreinoSerieRealizada[] history = CreateTopRangeHistory(exercise.Id, 70m, 10, 9, exercise.TreinoUsuarioId);

        TreinoProgressionSuggestion? suggestion = TreinoProgressionAnalyzer.Suggest(exercise, history);

        Assert.Null(suggestion);
    }

    private static TreinoUsuario CreateWorkout()
    {
        Exercicio catalog = CreateExercise("Supino reto", "Peitoral");
        var workout = new TreinoUsuario
        {
            TenantId = TenantId,
            UsuarioId = UserId,
            Nome = "Treino base",
            Objetivo = "Hipertrofia",
            FrequenciaSemanal = 3,
            Versao = 1,
            DataInicio = new DateOnly(2026, 8, 1),
            DuracaoSemanas = 6,
            Fase = "Fase 1",
            Ativo = true
        };

        workout.Exercicios.Add(new TreinoExercicio
        {
            TenantId = TenantId,
            TreinoUsuarioId = workout.Id,
            TreinoUsuario = workout,
            ExercicioId = catalog.Id,
            Exercicio = catalog,
            DiaTreino = 1,
            Ordem = 1,
            Series = 3,
            Repeticoes = "8-12",
            DescansoSegundos = 60
        });

        return workout;
    }

    private static TreinoExercicio CreatePrescribedExercise(
        TreinoUsuario workout,
        byte day,
        int order,
        string name)
    {
        Exercicio catalog = CreateExercise(name, "Peitoral");
        return new TreinoExercicio
        {
            TenantId = TenantId,
            TreinoUsuarioId = workout.Id,
            TreinoUsuario = workout,
            ExercicioId = catalog.Id,
            Exercicio = catalog,
            DiaTreino = day,
            Ordem = order,
            Series = 3,
            Repeticoes = "8-12",
            DescansoSegundos = 60
        };
    }

    private static AtualizarTreinoExercicioRequest CreateExerciseUpdateRequest(
        byte day,
        int order)
    {
        return new AtualizarTreinoExercicioRequest(
            day,
            order,
            3,
            "8-12",
            60,
            "Barra",
            null,
            null,
            null,
            null,
            null,
            null);
    }

    private static Exercicio CreateExercise(string name, string muscleGroup)
    {
        return new Exercicio
        {
            TenantId = TenantId,
            Nome = name,
            GrupoMuscular = muscleGroup,
            Nivel = "Intermediário",
            Equipamento = "Barra",
            Instrucao = "Execute com controle e sem dor."
        };
    }

    private static TreinoSerieRealizada[] CreateTopRangeHistory(Guid exerciseId, decimal load, int repetitions, byte rpe, Guid workoutId)
    {
        var firstSession = new TreinoSessao
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            UsuarioId = UserId,
            TreinoUsuarioId = workoutId,
            Data = new DateOnly(2026, 8, 1),
            DiaTreino = 1
        };
        var secondSession = new TreinoSessao
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            UsuarioId = UserId,
            TreinoUsuarioId = workoutId,
            Data = new DateOnly(2026, 8, 8),
            DiaTreino = 1
        };

        return
        [
            new TreinoSerieRealizada
            {
                TenantId = TenantId,
                TreinoSessaoId = secondSession.Id,
                Sessao = secondSession,
                TreinoExercicioId = exerciseId,
                NumeroSerie = 1,
                CargaKg = load,
                RepeticoesRealizadas = repetitions,
                Rpe = rpe
            },
            new TreinoSerieRealizada
            {
                TenantId = TenantId,
                TreinoSessaoId = firstSession.Id,
                Sessao = firstSession,
                TreinoExercicioId = exerciseId,
                NumeroSerie = 1,
                CargaKg = load,
                RepeticoesRealizadas = repetitions,
                Rpe = rpe
            }
        ];
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }

    private sealed class ConflictingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            throw new PersistenceConflictException(
                "persistence.concurrency",
                "Conflito de teste.");
        }
    }

    private sealed class FakeTreinoRepository : ITreinoRepository
    {
        public FakeTreinoRepository(TreinoUsuario workout)
        {
            Workout = workout;
            Catalog[workout.Exercicios.Single().ExercicioId] = workout.Exercicios.Single().Exercicio!;
        }

        public TreinoUsuario Workout { get; }

        public Dictionary<Guid, Exercicio> Catalog { get; } = [];

        public List<Exercicio> Substitutes { get; init; } = [];

        public TreinoEvolucaoProposta? Proposal { get; set; }

        public TreinoProgressaoSugestao? Suggestion { get; set; }

        public List<TreinoSerieRealizada> History { get; } = [];

        public List<TreinoUsuario> AddedWorkouts { get; } = [];

        public List<TreinoEvolucaoProposta> AddedProposals { get; } = [];

        public List<TreinoSessao> AddedSessions { get; } = [];

        public List<TreinoExercicio> AddedPrescriptions { get; } = [];

        public List<TreinoExercicioConclusao> Conclusions { get; } = [];

        public void AdicionarTreino(TreinoUsuario treinoUsuario) => AddedWorkouts.Add(treinoUsuario);

        public void AdicionarPropostaEvolucao(TreinoEvolucaoProposta proposta)
        {
            Proposal = proposta;
            AddedProposals.Add(proposta);
        }

        public void AdicionarExercicio(Exercicio exercicio) => Catalog[exercicio.Id] = exercicio;

        public void AdicionarTreinoExercicio(TreinoExercicio treinoExercicio)
        {
            AddedPrescriptions.Add(treinoExercicio);
            treinoExercicio.Exercicio = Catalog.GetValueOrDefault(treinoExercicio.ExercicioId);
            Workout.Exercicios.Add(treinoExercicio);
        }

        public void AdicionarSessao(TreinoSessao sessao) => AddedSessions.Add(sessao);

        public void AdicionarProgressaoSugestao(TreinoProgressaoSugestao sugestao) => Suggestion = sugestao;

        public Task<TreinoSessao?> ObterSessaoPorOperacaoAsync(Guid tenantId, Guid usuarioId, Guid operationId, CancellationToken cancellationToken)
        {
            TreinoSessao? session = AddedSessions.FirstOrDefault(item =>
                item.TenantId == tenantId &&
                item.UsuarioId == usuarioId &&
                item.OperationId == operationId);
            return Task.FromResult(session);
        }

        public Task<TreinoUsuario?> ObterTreinoPorIdAsync(Guid tenantId, Guid usuarioId, Guid treinoId, CancellationToken cancellationToken)
        {
            TreinoUsuario? workout = Workout.TenantId == tenantId && Workout.UsuarioId == usuarioId && Workout.Id == treinoId ? Workout : null;
            return Task.FromResult(workout);
        }

        public Task<TreinoUsuario?> ObterTreinoAtivoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
        {
            TreinoUsuario? workout = Workout.TenantId == tenantId && Workout.UsuarioId == usuarioId && Workout.Ativo ? Workout : null;
            return Task.FromResult(workout);
        }

        public Task<int> ObterMaiorVersaoAsync(Guid tenantId, Guid usuarioId, CancellationToken cancellationToken)
        {
            int version = new[] { Workout }.Concat(AddedWorkouts)
                .Where(workout => workout.TenantId == tenantId && workout.UsuarioId == usuarioId)
                .Select(workout => workout.Versao)
                .DefaultIfEmpty()
                .Max();
            return Task.FromResult(version);
        }

        public Task<TreinoExercicio?> ObterTreinoExercicioAsync(Guid tenantId, Guid usuarioId, Guid treinoId, Guid treinoExercicioId, CancellationToken cancellationToken)
        {
            TreinoExercicio? exercise = Workout.TenantId == tenantId && Workout.UsuarioId == usuarioId && Workout.Id == treinoId
                ? Workout.Exercicios.FirstOrDefault(item => item.Id == treinoExercicioId)
                : null;
            return Task.FromResult(exercise);
        }

        public Task<TreinoEvolucaoProposta?> ObterPropostaEvolucaoAsync(Guid tenantId, Guid usuarioId, Guid propostaId, CancellationToken cancellationToken)
        {
            TreinoEvolucaoProposta? proposal = Proposal is not null && Proposal.TenantId == tenantId && Proposal.UsuarioId == usuarioId && Proposal.Id == propostaId
                ? Proposal
                : null;
            return Task.FromResult(proposal);
        }

        public Task<TreinoProgressaoSugestao?> ObterProgressaoSugestaoAsync(Guid tenantId, Guid usuarioId, Guid sugestaoId, CancellationToken cancellationToken)
        {
            TreinoProgressaoSugestao? suggestion = Suggestion is not null && Suggestion.TenantId == tenantId && Suggestion.UsuarioId == usuarioId && Suggestion.Id == sugestaoId
                ? Suggestion
                : null;
            return Task.FromResult(suggestion);
        }

        public Task<PagedResult<TreinoUsuario>> ListarTreinosAsync(Guid tenantId, Guid usuarioId, bool? ativo, int page, int pageSize, CancellationToken cancellationToken)
        {
            TreinoUsuario[] items = [.. new[] { Workout }.Concat(AddedWorkouts).Where(workout => workout.TenantId == tenantId && workout.UsuarioId == usuarioId && (!ativo.HasValue || workout.Ativo == ativo.Value))];
            return Task.FromResult(new PagedResult<TreinoUsuario>(items, page, pageSize, items.Length));
        }

        public Task<IReadOnlyDictionary<Guid, Exercicio>> ObterExerciciosPorIdsAsync(Guid tenantId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<Guid, Exercicio> items = Catalog
                .Where(item => ids.Contains(item.Key) && item.Value.TenantId == tenantId)
                .ToDictionary(item => item.Key, item => item.Value);
            return Task.FromResult(items);
        }

        public Task<PagedResult<Exercicio>> ListarExerciciosAsync(Guid tenantId, string? termo, int page, int pageSize, CancellationToken cancellationToken)
        {
            Exercicio[] items = [.. Catalog.Values.Where(exercise => exercise.TenantId == tenantId)];
            return Task.FromResult(new PagedResult<Exercicio>(items, page, pageSize, items.Length));
        }

        public Task<IReadOnlyCollection<Exercicio>> ListarSubstitutosAsync(Guid tenantId, Guid exercicioAtualId, string grupoMuscular, string? equipamento, int take, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<Exercicio> items = Substitutes
                .Where(exercise => exercise.TenantId == tenantId && exercise.Id != exercicioAtualId && exercise.GrupoMuscular == grupoMuscular)
                .Take(take)
                .ToArray();
            return Task.FromResult(items);
        }

        public Task<IReadOnlyCollection<TreinoExercicioConclusao>> ListarConclusoesAsync(Guid tenantId, Guid usuarioId, Guid treinoId, DateOnly data, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<TreinoExercicioConclusao> items = Conclusions
                .Where(item => item.TenantId == tenantId && item.UsuarioId == usuarioId && item.TreinoUsuarioId == treinoId && item.Data == data)
                .ToArray();
            return Task.FromResult(items);
        }

        public Task<IReadOnlyCollection<TreinoSerieRealizada>> ListarSeriesPorTreinoAsync(Guid tenantId, Guid usuarioId, Guid treinoId, int take, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<TreinoSerieRealizada> items = History
                .Where(item => item.TenantId == tenantId && item.Sessao?.UsuarioId == usuarioId && item.Sessao.TreinoUsuarioId == treinoId)
                .Take(take)
                .ToArray();
            return Task.FromResult(items);
        }

        public Task<IReadOnlyCollection<TreinoSerieRealizada>> ListarSeriesPorExercicioAsync(Guid tenantId, Guid usuarioId, Guid exercicioId, int take, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<TreinoSerieRealizada> items = History
                .Where(item => item.TenantId == tenantId && item.Sessao?.UsuarioId == usuarioId && item.TreinoExercicio?.ExercicioId == exercicioId)
                .Take(take)
                .ToArray();
            return Task.FromResult(items);
        }

        public Task<TreinoExercicioConclusao?> ObterConclusaoAsync(Guid tenantId, Guid usuarioId, Guid treinoExercicioId, DateOnly data, CancellationToken cancellationToken)
        {
            TreinoExercicioConclusao? conclusion = Conclusions.FirstOrDefault(item =>
                item.TenantId == tenantId &&
                item.UsuarioId == usuarioId &&
                item.TreinoExercicioId == treinoExercicioId &&
                item.Data == data);
            return Task.FromResult(conclusion);
        }

        public void AdicionarConclusao(TreinoExercicioConclusao conclusao) => Conclusions.Add(conclusao);
    }
}
