using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Infrastructure.Data.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Data;

public sealed class RelationalDatabaseTests
{
    [Fact]
    public async Task Sqlite_ShouldCreateCompleteSchemaAndSeed()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync();
        Assert.True(db.Model.GetEntityTypes().Count() >= 30);
        Assert.True(await db.Tenants.AnyAsync(x => x.Id == SeedData.DefaultTenantId));
    }

    [Fact]
    public async Task Sqlite_ShouldRejectDuplicateActivePositionAndAllowReplacementAfterSoftDelete()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync();
        await VerifySoftDeleteAsync(db);
    }

    [Fact]
    public async Task Sqlite_ShouldDetectConcurrentWrites()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var first = CreateContext(connection);
        await first.Database.EnsureCreatedAsync();
        var workout = await CreateWorkoutAsync(first);
        await using var second = CreateContext(connection);
        var stale = await second.TreinosUsuario.SingleAsync(x => x.Id == workout.Id);
        workout.Nome = "Atualizacao A";
        await first.SaveChangesAsync();
        stale.Nome = "Atualizacao B";
        var conflict = await Assert.ThrowsAsync<PersistenceConflictException>(() => second.SaveChangesAsync());
        Assert.Equal("persistence.concurrency", conflict.Code);
    }

    [Fact]
    public async Task Sqlite_ShouldEnforceOperationIdAndForeignKeys()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = CreateContext(connection);
        await db.Database.EnsureCreatedAsync();
        var workout = await CreateWorkoutAsync(db);
        var operation = Guid.NewGuid();
        db.TreinosSessoes.Add(new TreinoSessao
        {
            TenantId = workout.TenantId, UsuarioId = workout.UsuarioId,
            TreinoUsuarioId = workout.Id, DiaTreino = 1, OperationId = operation
        });
        await db.SaveChangesAsync();
        db.TreinosSessoes.Add(new TreinoSessao
        {
            TenantId = workout.TenantId, UsuarioId = workout.UsuarioId,
            TreinoUsuarioId = workout.Id, DiaTreino = 1, OperationId = operation
        });
        await Assert.ThrowsAsync<PersistenceConflictException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.TreinosExercicios.Add(new TreinoExercicio
        {
            TenantId = workout.TenantId, TreinoUsuarioId = Guid.NewGuid(),
            ExercicioId = Guid.NewGuid(), Ordem = 1, Series = 3, DescansoSegundos = 60
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Sqlite_ShouldOrderUtcDatesAndPersistAcrossContextRestart()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var db = CreateContext(connection))
        {
            await db.Database.EnsureCreatedAsync();
            var workout = await CreateWorkoutAsync(db);
            workout.Usuario!.UltimoLoginEm = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.FromHours(-3));
            await db.SaveChangesAsync();
        }
        await using var reopened = CreateContext(connection);
        var user = await reopened.Usuarios.OrderBy(x => x.UltimoLoginEm).SingleAsync();
        Assert.Equal(new DateTimeOffset(2026, 8, 1, 13, 0, 0, TimeSpan.Zero), user.UltimoLoginEm);
    }

    internal static async Task VerifySoftDeleteAsync(EquilibraFitPlusPlusDbContext db)
    {
        var workout = await CreateWorkoutAsync(db);
        var active = workout.Exercicios.Single();
        var duplicate = new TreinoExercicio
        {
            TenantId = workout.TenantId, TreinoUsuarioId = workout.Id,
            ExercicioId = active.ExercicioId, DiaTreino = 1, Ordem = 1,
            Series = 3, Repeticoes = "8-12", DescansoSegundos = 60
        };
        db.TreinosExercicios.Add(duplicate);
        var conflict = await Assert.ThrowsAsync<PersistenceConflictException>(() => db.SaveChangesAsync());
        Assert.Equal("persistence.unique_constraint", conflict.Code);
        db.Entry(duplicate).State = EntityState.Detached;
        active.ExcluidoEm = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        db.TreinosExercicios.Add(duplicate);
        await db.SaveChangesAsync();
        Assert.Single(await db.TreinosExercicios.Where(x => x.TreinoUsuarioId == workout.Id).ToArrayAsync());
        Assert.Equal(2, await db.TreinosExercicios.IgnoreQueryFilters().CountAsync(x => x.TreinoUsuarioId == workout.Id));
    }

    private static EquilibraFitPlusPlusDbContext CreateContext(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>().UseSqlite(connection).Options);

    private static async Task<TreinoUsuario> CreateWorkoutAsync(EquilibraFitPlusPlusDbContext db)
    {
        var user = new Usuario
        {
            TenantId = SeedData.DefaultTenantId, IdentityUserId = Guid.NewGuid(),
            Nome = "Teste relacional", Email = $"{Guid.NewGuid():N}@example.test"
        };
        var exercise = new Exercicio { TenantId = user.TenantId, Nome = "Supino", GrupoMuscular = "Peitoral" };
        var workout = new TreinoUsuario
        {
            TenantId = user.TenantId, UsuarioId = user.Id, Usuario = user,
            Nome = "Treino de teste", Objetivo = "Forca", FrequenciaSemanal = 3
        };
        workout.Exercicios.Add(new TreinoExercicio
        {
            TenantId = user.TenantId, ExercicioId = exercise.Id, Exercicio = exercise,
            DiaTreino = 1, Ordem = 1, Series = 3, Repeticoes = "8-12", DescansoSegundos = 60
        });
        db.Add(workout);
        await db.SaveChangesAsync();
        return workout;
    }
}
