using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Data;

/// <summary>
/// Tests workout repository ownership filters.
/// </summary>
public sealed class TreinoRepositoryOwnershipTests
{
    /// <summary>
    /// Ensures another user cannot read workout plans, sessions, history, proposals, or progressions.
    /// </summary>
    [Fact]
    public async Task Repository_ShouldFilterProgressiveWorkoutDataByTenantAndUser()
    {
        Guid tenantId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        Guid intruderId = Guid.NewGuid();
        await using EquilibraFitPlusPlusDbContext dbContext = CreateDbContext();
        Tenant tenant = new()
        {
            Id = tenantId,
            Nome = "Tenant teste",
            Tipo = "Individual"
        };
        Usuario owner = CreateUser(tenantId, ownerId, "owner@equilibrafit.test");
        Usuario intruder = CreateUser(tenantId, intruderId, "intruder@equilibrafit.test");
        TreinoUsuario workout = CreateWorkout(tenantId, ownerId);
        workout.Usuario = owner;
        TreinoExercicio prescribed = workout.Exercicios.Single();
        TreinoSessao session = CreateSession(tenantId, ownerId, workout, prescribed);
        session.Usuario = owner;
        var proposal = new TreinoEvolucaoProposta
        {
            TenantId = tenantId,
            UsuarioId = ownerId,
            Usuario = owner,
            TreinoUsuarioId = workout.Id,
            TreinoUsuario = workout,
            VersaoProposta = 2,
            PlanoJson = "{}",
            MudancasResumo = "Progressão segura.",
            Status = "Pendente"
        };
        var progression = new TreinoProgressaoSugestao
        {
            TenantId = tenantId,
            UsuarioId = ownerId,
            Usuario = owner,
            TreinoUsuarioId = workout.Id,
            TreinoUsuario = workout,
            TreinoExercicioId = prescribed.Id,
            TreinoExercicio = prescribed,
            CargaAtualKg = 70m,
            CargaSugeridaKg = 72.5m,
            Motivo = "Topo da faixa em duas sessões.",
            Status = "Pendente"
        };

        dbContext.Add(tenant);
        dbContext.Add(owner);
        dbContext.Add(intruder);
        dbContext.Add(workout.Exercicios.Single().Exercicio!);
        dbContext.Add(workout);
        dbContext.Add(session);
        dbContext.Add(proposal);
        dbContext.Add(progression);
        await dbContext.SaveChangesAsync();
        var repository = new TreinoRepository(dbContext);

        Assert.Null(await repository.ObterTreinoPorIdAsync(tenantId, intruderId, workout.Id, CancellationToken.None));
        Assert.Null(await repository.ObterTreinoExercicioAsync(tenantId, intruderId, workout.Id, prescribed.Id, CancellationToken.None));
        Assert.Null(await repository.ObterPropostaEvolucaoAsync(tenantId, intruderId, proposal.Id, CancellationToken.None));
        Assert.Null(await repository.ObterProgressaoSugestaoAsync(tenantId, intruderId, progression.Id, CancellationToken.None));
        Assert.Empty(await repository.ListarSeriesPorTreinoAsync(tenantId, intruderId, workout.Id, 10, CancellationToken.None));
        Assert.Empty(await repository.ListarSeriesPorExercicioAsync(tenantId, intruderId, prescribed.ExercicioId, 10, CancellationToken.None));

        Assert.NotNull(await repository.ObterTreinoPorIdAsync(tenantId, ownerId, workout.Id, CancellationToken.None));
        Assert.Single(await repository.ListarSeriesPorTreinoAsync(tenantId, ownerId, workout.Id, 10, CancellationToken.None));
    }

    private static EquilibraFitPlusPlusDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EquilibraFitPlusPlusDbContext(options);
    }

    private static Usuario CreateUser(Guid tenantId, Guid userId, string email)
    {
        return new Usuario
        {
            Id = userId,
            TenantId = tenantId,
            IdentityUserId = Guid.NewGuid(),
            Nome = email,
            Email = email
        };
    }

    private static TreinoUsuario CreateWorkout(Guid tenantId, Guid ownerId)
    {
        var exercise = new Exercicio
        {
            TenantId = tenantId,
            Nome = "Supino reto",
            GrupoMuscular = "Peitoral",
            Nivel = "Intermediário",
            Equipamento = "Barra",
            Instrucao = "Execute com controle."
        };
        var workout = new TreinoUsuario
        {
            TenantId = tenantId,
            UsuarioId = ownerId,
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
            TenantId = tenantId,
            TreinoUsuarioId = workout.Id,
            TreinoUsuario = workout,
            ExercicioId = exercise.Id,
            Exercicio = exercise,
            DiaTreino = 1,
            Ordem = 1,
            Series = 3,
            Repeticoes = "8-12",
            DescansoSegundos = 60
        });

        return workout;
    }

    private static TreinoSessao CreateSession(Guid tenantId, Guid ownerId, TreinoUsuario workout, TreinoExercicio prescribed)
    {
        var session = new TreinoSessao
        {
            TenantId = tenantId,
            UsuarioId = ownerId,
            TreinoUsuarioId = workout.Id,
            TreinoUsuario = workout,
            DiaTreino = 1,
            Data = new DateOnly(2026, 8, 8),
            Resumo = "Sessão registrada."
        };
        session.Series.Add(new TreinoSerieRealizada
        {
            TenantId = tenantId,
            TreinoSessaoId = session.Id,
            Sessao = session,
            TreinoExercicioId = prescribed.Id,
            TreinoExercicio = prescribed,
            NumeroSerie = 1,
            CargaKg = 70m,
            RepeticoesRealizadas = 10,
            Rpe = 7
        });

        return session;
    }
}
