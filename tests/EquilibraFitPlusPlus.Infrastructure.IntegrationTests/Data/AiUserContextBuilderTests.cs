using EquilibraFitPlusPlus.Application.Abstractions.AiContext;
using EquilibraFitPlusPlus.Application.Common.Serialization;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Infrastructure.AiContext;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Data;

/// <summary>
/// Tests trusted AI user context construction.
/// </summary>
public sealed class AiUserContextBuilderTests
{
    /// <summary>
    /// Ensures a complete workout context is assembled from owned persisted data only.
    /// </summary>
    [Fact]
    public async Task BuildWorkoutContext_ShouldIncludeCompleteContextFromOwnedData()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await using EquilibraFitPlusPlusDbContext dbContext = CreateDbContext();
        SeedBaseUser(dbContext, tenantId, userId);
        dbContext.PerfisSaude.Add(CreateProfile(tenantId, userId, ["joelho sensível"]));
        TreinoUsuario workout = CreateWorkout(tenantId, userId, "Supino com halteres", "Halteres");
        TreinoExercicio exercise = workout.Exercicios.Single();
        dbContext.TreinosUsuario.Add(workout);
        dbContext.TreinosSessoes.Add(CreateSession(tenantId, userId, workout, exercise, false, 7, 20m));
        dbContext.TreinosExerciciosConclusoes.AddRange(
            CreateCompletion(tenantId, userId, workout, exercise, false, DateTime.UtcNow.AddDays(-4)),
            CreateCompletion(tenantId, userId, workout, exercise, false, DateTime.UtcNow.AddDays(-2)));
        dbContext.RegistrosEvolucao.AddRange(
            CreateEvolution(tenantId, userId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-20)), 82m, 24m),
            CreateEvolution(tenantId, userId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), 80m, 22m));
        dbContext.RegistrosHabitos.Add(CreateHabits(tenantId, userId));
        dbContext.AiCoachMemories.AddRange(
            CreateMemory(tenantId, userId, "PrioridadeTreino", "prioridade_bracos", "braços", true),
            CreateMemory(tenantId, userId, "PreferenciaEquipamento", "inferida_maquinas", "máquinas", false, AiCoachMemoryFactTypes.AiInference));
        await dbContext.SaveChangesAsync();

        var builder = new AiUserContextBuilder(dbContext);
        var context = await builder.BuildWorkoutContextAsync(tenantId, userId, CancellationToken.None);

        Assert.Equal(1, context.ContextVersion);
        Assert.NotNull(context.Perfil);
        Assert.Equal(nameof(SexoBiologico.NaoInformado), context.Perfil!.Sexo);
        Assert.Equal(180m, context.Perfil!.AlturaCm);
        Assert.Equal(78m, context.Perfil.PercentualMassaMagra);
        Assert.Equal(80m, context.Evolucao!.PesoAtualKg);
        Assert.Equal("descendo", context.Evolucao.TendenciaPeso);
        Assert.Contains(context.Evolucao.MedidasRelevantes, medida => medida.Nome == "Cintura");
        Assert.Equal(workout.Id, context.PlanoAtual!.Id);
        Assert.Equal(4, context.PlanoAtual.DiasPorSemana);
        Assert.Single(context.PlanoAtual.ExerciciosPrescritos);
        AiPrescribedExerciseContext prescribed = Assert.Single(context.PlanoAtual.ExerciciosPrescritos);
        Assert.Equal(exercise.ExercicioId, prescribed.ExercicioId);
        Assert.Equal(8, prescribed.RepeticoesMin);
        Assert.Equal(12, prescribed.RepeticoesMax);
        Assert.Equal("Manter carga ate fechar 12 reps.", prescribed.ProgressaoAtual);
        Assert.Equal("Halteres", Assert.Single(context.Rotina!.Equipamentos));
        Assert.Contains("joelho sensível", context.Seguranca!.LimitacoesAtuais);
        Assert.Equal(1, context.HistoricoRecente!.Sessoes);
        Assert.Equal(18, context.HistoricoRecente.SessoesPrevistas);
        Assert.Equal(6.00m, context.HistoricoRecente.AderenciaPercentual);
        Assert.Equal(7m, context.HistoricoRecente.RpeMedio);
        Assert.Equal(200m, context.HistoricoRecente.VolumeTotalKg);
        Assert.Contains(context.HistoricoRecente.MelhoresCargas, carga => carga.Nome == "Supino com halteres" && carga.CargaKg == 20m);
        Assert.Contains(context.HistoricoRecente.UltimasCargas, carga => carga.Nome == "Supino com halteres" && carga.CargaKg == 20m);
        Assert.Contains("Supino com halteres", context.HistoricoRecente.ExerciciosFrequentementeNaoConcluidos);
        Assert.NotNull(context.Recuperacao);
        Assert.Equal(7m, context.Recuperacao.SonoRecenteHoras);
        Assert.Equal(2100, context.Recuperacao.AguaRecenteMl);
        Assert.Contains(context.PreferenciasRelevantes, memory => memory.Chave == "prioridade_bracos");
        Assert.DoesNotContain(context.PreferenciasRelevantes, memory => memory.Chave == "inferida_maquinas");
    }

    /// <summary>
    /// Ensures partial context keeps absent data absent instead of inventing history.
    /// </summary>
    [Fact]
    public async Task BuildWorkoutContext_ShouldHandlePartialUserWithoutHistory()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await using EquilibraFitPlusPlusDbContext dbContext = CreateDbContext();
        SeedBaseUser(dbContext, tenantId, userId);
        dbContext.PerfisSaude.Add(CreateProfile(tenantId, userId, []));
        await dbContext.SaveChangesAsync();

        var builder = new AiUserContextBuilder(dbContext);
        var context = await builder.BuildWorkoutContextAsync(tenantId, userId, CancellationToken.None);

        Assert.NotNull(context.Perfil);
        Assert.Null(context.PlanoAtual);
        Assert.Null(context.HistoricoRecente);
        Assert.Null(context.Recuperacao);
        Assert.Empty(context.PreferenciasRelevantes);
    }

    /// <summary>
    /// Ensures recent pain and high RPE are visible to AI safety decisions.
    /// </summary>
    [Fact]
    public async Task BuildWorkoutContext_ShouldExposeRecentPainAndHighRpe()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await using EquilibraFitPlusPlusDbContext dbContext = CreateDbContext();
        SeedBaseUser(dbContext, tenantId, userId);
        dbContext.PerfisSaude.Add(CreateProfile(tenantId, userId, []));
        TreinoUsuario workout = CreateWorkout(tenantId, userId, "Agachamento", "Barra");
        dbContext.TreinosUsuario.Add(workout);
        dbContext.TreinosSessoes.Add(CreateSession(tenantId, userId, workout, workout.Exercicios.Single(), true, 9, 70m));
        await dbContext.SaveChangesAsync();

        var builder = new AiUserContextBuilder(dbContext);
        var context = await builder.BuildWorkoutContextAsync(tenantId, userId, CancellationToken.None);

        Assert.True(context.Seguranca!.DorDesconfortoRecente);
        Assert.Equal((byte)9, context.HistoricoRecente!.RpeMaximo);
        Assert.Contains("joelho", string.Join(' ', context.Seguranca.DescricoesDorRecentes), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Ensures the lightweight coach history still exposes safety data from performed series.
    /// </summary>
    [Fact]
    public async Task BuildCoachContext_ShouldExposePainWithoutWorkoutHistoryDetails()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await using EquilibraFitPlusPlusDbContext dbContext = CreateDbContext();
        SeedBaseUser(dbContext, tenantId, userId);
        dbContext.PerfisSaude.Add(CreateProfile(tenantId, userId, []));
        TreinoUsuario workout = CreateWorkout(tenantId, userId, "Agachamento", "Barra");
        dbContext.TreinosUsuario.Add(workout);
        dbContext.TreinosSessoes.Add(CreateSession(
            tenantId,
            userId,
            workout,
            workout.Exercicios.Single(),
            true,
            9,
            70m));
        dbContext.RegistrosHabitos.Add(CreateHabits(tenantId, userId));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var builder = new AiUserContextBuilder(dbContext);
        var context = await builder.BuildCoachContextAsync(tenantId, userId, CancellationToken.None);

        Assert.NotNull(context.TreinoAtual);
        Assert.True(context.SegurancaTreino!.DorDesconfortoRecente);
        Assert.Contains(
            "joelho",
            string.Join(' ', context.SegurancaTreino.DescricoesDorRecentes),
            StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(context.Recuperacao);
    }

    /// <summary>
    /// Ensures progression summaries are built from owned recent series.
    /// </summary>
    [Fact]
    public async Task BuildWorkoutContext_ShouldSummarizeProgressionFromHistory()
    {
        Guid tenantId = Guid.NewGuid();
        Guid userId = Guid.NewGuid();
        await using EquilibraFitPlusPlusDbContext dbContext = CreateDbContext();
        SeedBaseUser(dbContext, tenantId, userId);
        dbContext.PerfisSaude.Add(CreateProfile(tenantId, userId, []));
        TreinoUsuario workout = CreateWorkout(tenantId, userId, "Remada", "Halteres");
        TreinoExercicio exercise = workout.Exercicios.Single();
        dbContext.TreinosUsuario.Add(workout);
        dbContext.TreinosSessoes.Add(CreateSession(tenantId, userId, workout, exercise, false, 7, 20m, DateTime.UtcNow.AddDays(-12)));
        dbContext.TreinosSessoes.Add(CreateSession(tenantId, userId, workout, exercise, false, 7, 24m, DateTime.UtcNow.AddDays(-2)));
        await dbContext.SaveChangesAsync();

        var builder = new AiUserContextBuilder(dbContext);
        var context = await builder.BuildWorkoutContextAsync(tenantId, userId, CancellationToken.None);

        var progression = Assert.Single(context.HistoricoRecente!.Progressao);
        Assert.Equal("Remada", progression.Nome);
        Assert.Equal("subindo", progression.Tendencia);
        Assert.Equal(24m, progression.CargaRecenteKg);
    }

    /// <summary>
    /// Ensures context queries are isolated by tenant and user.
    /// </summary>
    [Fact]
    public async Task BuildWorkoutContext_ShouldIsolateTenantAndUser()
    {
        Guid tenantId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        Guid intruderId = Guid.NewGuid();
        await using EquilibraFitPlusPlusDbContext dbContext = CreateDbContext();
        SeedBaseUser(dbContext, tenantId, ownerId);
        SeedUser(dbContext, tenantId, intruderId, "intruder@equilibrafit.test");
        dbContext.PerfisSaude.Add(CreateProfile(tenantId, ownerId, []));
        dbContext.PerfisSaude.Add(CreateProfile(tenantId, intruderId, []));
        dbContext.TreinosUsuario.Add(CreateWorkout(tenantId, ownerId, "Supino", "Halteres"));
        dbContext.TreinosUsuario.Add(CreateWorkout(tenantId, intruderId, "Exercicio privado", "Barra"));
        await dbContext.SaveChangesAsync();

        var builder = new AiUserContextBuilder(dbContext);
        var context = await builder.BuildWorkoutContextAsync(tenantId, ownerId, CancellationToken.None);

        Assert.DoesNotContain(
            context.PlanoAtual!.ExerciciosPrescritos,
            exercise => exercise.Nome == "Exercicio privado");
    }

    private static EquilibraFitPlusPlusDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EquilibraFitPlusPlusDbContext(options);
    }

    private static void SeedBaseUser(EquilibraFitPlusPlusDbContext dbContext, Guid tenantId, Guid userId)
    {
        dbContext.Tenants.Add(new Tenant { Id = tenantId, Nome = "Tenant teste", Tipo = "Individual" });
        SeedUser(dbContext, tenantId, userId, "owner@equilibrafit.test");
    }

    private static void SeedUser(EquilibraFitPlusPlusDbContext dbContext, Guid tenantId, Guid userId, string email)
    {
        dbContext.Usuarios.Add(new Usuario
        {
            Id = userId,
            TenantId = tenantId,
            IdentityUserId = Guid.NewGuid(),
            Nome = email,
            Email = email
        });
    }

    private static PerfilSaude CreateProfile(Guid tenantId, Guid userId, IReadOnlyCollection<string> observations)
    {
        return new PerfilSaude
        {
            TenantId = tenantId,
            UsuarioId = userId,
            DataNascimento = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-32)),
            SexoBiologico = SexoBiologico.NaoInformado,
            AlturaCm = 180m,
            PesoAtualKg = 82m,
            Objetivo = ObjetivoSaude.GanhoMassa,
            NivelAtividade = NivelAtividade.Moderado,
            DiasTreinoSemana = 4,
            ObservacoesJson = JsonStringCollection.Serialize(observations)
        };
    }

    private static TreinoUsuario CreateWorkout(Guid tenantId, Guid userId, string exerciseName, string equipment)
    {
        var exercise = new Exercicio
        {
            TenantId = tenantId,
            Nome = exerciseName,
            GrupoMuscular = "Costas",
            Nivel = "Intermediario",
            Equipamento = equipment,
            Instrucao = "Execute com controle."
        };
        var workout = new TreinoUsuario
        {
            TenantId = tenantId,
            UsuarioId = userId,
            Nome = "Treino base",
            Objetivo = "Hipertrofia",
            FrequenciaSemanal = 4,
            Versao = 1,
            DataInicio = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)),
            DuracaoSemanas = 6,
            Fase = "Fase 1",
            Ativo = true
        };
        workout.Exercicios.Add(new TreinoExercicio
        {
            TenantId = tenantId,
            TreinoUsuarioId = workout.Id,
            TreinoUsuario = workout,
            ExercicioId = exercise.Id,
            Exercicio = exercise,
            DiaTreino = 1,
            Ordem = 1,
            Series = 3,
            Repeticoes = "8-12",
            DescansoSegundos = 90,
            CargaAlvoKg = 20m,
            RpeAlvo = 8,
            RepeticoesMin = 8,
            RepeticoesMax = 12,
            Observacao = "Executar com controle.",
            ProgressaoMotivo = "Manter carga ate fechar 12 reps."
        });

        return workout;
    }

    private static TreinoSessao CreateSession(
        Guid tenantId,
        Guid userId,
        TreinoUsuario workout,
        TreinoExercicio exercise,
        bool pain,
        byte rpe,
        decimal load,
        DateTime? date = null)
    {
        var session = new TreinoSessao
        {
            TenantId = tenantId,
            UsuarioId = userId,
            TreinoUsuarioId = workout.Id,
            TreinoUsuario = workout,
            DiaTreino = 1,
            Data = DateOnly.FromDateTime(date ?? DateTime.UtcNow.AddDays(-2)),
            IniciadoEm = DateTimeOffset.UtcNow.AddDays(-2),
            FinalizadoEm = DateTimeOffset.UtcNow.AddDays(-2).AddMinutes(42)
        };
        session.Series.Add(new TreinoSerieRealizada
        {
            TenantId = tenantId,
            TreinoSessaoId = session.Id,
            Sessao = session,
            TreinoExercicioId = exercise.Id,
            TreinoExercicio = exercise,
            NumeroSerie = 1,
            CargaKg = load,
            RepeticoesRealizadas = 10,
            Rpe = rpe,
            DorDesconforto = pain,
            DorDescricao = pain ? "joelho sensível no agachamento" : null
        });

        return session;
    }

    private static TreinoExercicioConclusao CreateCompletion(
        Guid tenantId,
        Guid userId,
        TreinoUsuario workout,
        TreinoExercicio exercise,
        bool completed,
        DateTime date)
    {
        return new TreinoExercicioConclusao
        {
            TenantId = tenantId,
            UsuarioId = userId,
            TreinoUsuarioId = workout.Id,
            TreinoUsuario = workout,
            TreinoExercicioId = exercise.Id,
            TreinoExercicio = exercise,
            Data = DateOnly.FromDateTime(date),
            Concluido = completed,
            ConcluidoEm = completed ? DateTimeOffset.UtcNow : null
        };
    }

    private static RegistroEvolucao CreateEvolution(Guid tenantId, Guid userId, DateOnly date, decimal weight, decimal bodyFat)
    {
        return new RegistroEvolucao
        {
            TenantId = tenantId,
            UsuarioId = userId,
            Data = date,
            PesoKg = weight,
            PercentualGordura = bodyFat,
            PercentualMassaMagra = 100m - bodyFat,
            Medidas =
            [
                new MedidaCorporal
                {
                    TenantId = tenantId,
                    Nome = "Cintura",
                    ValorCm = 90m
                }
            ]
        };
    }

    private static RegistroHabitos CreateHabits(Guid tenantId, Guid userId)
    {
        return new RegistroHabitos
        {
            TenantId = tenantId,
            UsuarioId = userId,
            Data = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            AguaMl = 2100,
            MetaAguaMl = 2700,
            SonoHoras = 7m,
            MetaSonoHoras = 8m,
            Humor = 4,
            AlongamentoRealizado = true
        };
    }

    private static AiCoachMemory CreateMemory(
        Guid tenantId,
        Guid userId,
        string category,
        string key,
        string value,
        bool confirmed,
        string factType = AiCoachMemoryFactTypes.ExplicitUserFact)
    {
        return new AiCoachMemory
        {
            TenantId = tenantId,
            UsuarioId = userId,
            Categoria = category,
            Chave = key,
            Valor = value,
            TipoFato = factType,
            ConfirmadoPeloUsuario = confirmed,
            Fonte = "Teste",
            ConfirmadoEm = confirmed ? DateTimeOffset.UtcNow : null
        };
    }
}
