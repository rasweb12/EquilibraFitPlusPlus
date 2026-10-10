using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Infrastructure.Data.Seed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Data;

public sealed class TreinoSerieQueryFilterTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Model_ShouldFilterSessionDependentsInBothProviders(bool postgres)
    {
        var warnings = new List<string>();
        var options = new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>()
            .LogTo(warnings.Add,
                [CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning],
                LogLevel.Warning);
        if (postgres)
        {
            options.UseNpgsql("Host=model.invalid;Database=filter_tests;Username=filter_tests");
        }
        else
        {
            options.UseSqlite("Data Source=:memory:");
        }

        using var db = new EquilibraFitPlusPlusDbContext(options.Options);
        var entity = db.Model.FindEntityType(typeof(TreinoSerieRealizada));
        Assert.NotNull(entity);
        Assert.NotEmpty(entity.GetDeclaredQueryFilters());
        Assert.NotEmpty(db.Model.FindEntityType(typeof(AuthSession))!.GetDeclaredQueryFilters());
        Assert.Contains(entity.GetForeignKeys(), foreignKey => foreignKey.IsRequired
            && foreignKey.PrincipalEntityType.ClrType == typeof(TreinoSessao));
        Assert.DoesNotContain(warnings, warning => warning.Contains(nameof(TreinoSerieRealizada), StringComparison.Ordinal)
            || warning.Contains(nameof(AuthSession), StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sqlite_ShouldReturnSameSeriesWithAndWithoutSessionInclude(bool deleteSecondUser)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>()
            .UseSqlite(connection).Options;
        await using var db = new EquilibraFitPlusPlusDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var active = CreateSession("active@example.test");
        var second = CreateSession("second@example.test");
        db.AddRange(active, second);
        db.AuthSessions.AddRange(
            new AuthSession { TenantId = active.TenantId, UsuarioId = active.UsuarioId, Usuario = active.Usuario, SessionId = Guid.NewGuid() },
            new AuthSession { TenantId = second.TenantId, UsuarioId = second.UsuarioId, Usuario = second.Usuario, SessionId = Guid.NewGuid() });
        await db.SaveChangesAsync();
        if (deleteSecondUser)
        {
            second.Usuario!.ExcluidoEm = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
        db.ChangeTracker.Clear();

        var direct = await db.TreinosSeriesRealizadas.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync();
        var included = await db.TreinosSeriesRealizadas.AsNoTracking()
            .Include(x => x.Sessao).OrderBy(x => x.Id).ToArrayAsync();
        Assert.Equal(deleteSecondUser ? 1 : 2, direct.Length);
        Assert.Equal(direct.Select(x => x.Id), included.Select(x => x.Id));
        Assert.Contains(direct, x => x.TreinoSessaoId == active.Id);
        Assert.Equal(2, await db.TreinosSeriesRealizadas.IgnoreQueryFilters().CountAsync());
        Assert.Equal(deleteSecondUser ? 1 : 2, await db.TreinosSessoes.CountAsync());
        Assert.Equal(deleteSecondUser ? 1 : 2, await db.AuthSessions.CountAsync());
        Assert.Equal(deleteSecondUser ? 1 : 2, await db.AuthSessions.Include(x => x.Usuario).CountAsync());
        Assert.Equal(2, await db.AuthSessions.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public void PostgreSql_ShouldApplyOwnerFilterToDirectSeriesQueryWithoutOpeningConnection()
    {
        var options = new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>()
            .UseNpgsql("Host=model.invalid;Database=filter_tests;Username=filter_tests").Options;
        using var db = new EquilibraFitPlusPlusDbContext(options);
        string sql = db.TreinosSeriesRealizadas.AsNoTracking().ToQueryString();
        Assert.Contains("\"Usuarios\"", sql);
        Assert.Contains("\"ExcluidoEm\" IS NULL", sql);
    }

    private static TreinoSessao CreateSession(string email)
    {
        var user = new Usuario
        {
            TenantId = SeedData.DefaultTenantId, IdentityUserId = Guid.NewGuid(),
            Nome = "Filter test", Email = email
        };
        var exercise = new Exercicio
        {
            TenantId = user.TenantId, Nome = "Supino", GrupoMuscular = "Peitoral"
        };
        var workout = new TreinoUsuario
        {
            TenantId = user.TenantId, UsuarioId = user.Id, Usuario = user,
            Nome = "Workout filter test", Objetivo = "Forca", FrequenciaSemanal = 3
        };
        var prescribed = new TreinoExercicio
        {
            TenantId = user.TenantId, TreinoUsuario = workout, Exercicio = exercise,
            DiaTreino = 1, Ordem = 1, Series = 3, Repeticoes = "8-12", DescansoSegundos = 60
        };
        workout.Exercicios.Add(prescribed);
        var session = new TreinoSessao
        {
            TenantId = user.TenantId, UsuarioId = user.Id, Usuario = user,
            TreinoUsuario = workout, DiaTreino = 1
        };
        session.Series.Add(new TreinoSerieRealizada
        {
            TenantId = user.TenantId, Sessao = session, TreinoExercicio = prescribed,
            NumeroSerie = 1, RepeticoesRealizadas = 10, Rpe = 7
        });
        return session;
    }
}
