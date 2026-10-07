using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Data;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING")))
        {
            Skip = "Configure TEST_POSTGRES_CONNECTION_STRING for a dedicated empty PostgreSQL test project.";
        }
    }
}

public sealed class PostgreSqlMigrationTests
{
    [Fact]
    public void Baseline_ShouldGeneratePostgreSqlTablesConstraintsAndPartialIndexes()
    {
        using var db = new EquilibraFitPlusPlusDbContext(
            new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>().UseNpgsql().Options);
        var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("CREATE TABLE app.", script);
        Assert.Contains("uuid", script);
        Assert.Contains("FOREIGN KEY", script);
        Assert.Contains("CHECK", script);
        Assert.Contains("WHERE \"ExcluidoEm\" IS NULL", script);
        Assert.Contains("WHERE \"OperationId\" IS NOT NULL", script);
        Assert.Contains("ALTER TABLE app.\"BillingEvents\" ENABLE ROW LEVEL SECURITY", script);
        Assert.Contains("DROP POLICY IF EXISTS own_read ON app.\"Assinaturas\"", script);
        Assert.Contains("ALTER TABLE public.\"__EFMigrationsHistory\" ENABLE ROW LEVEL SECURITY", script);
        Assert.Contains("REVOKE ALL ON public.\"__EFMigrationsHistory\" FROM PUBLIC", script);
        Assert.DoesNotContain("SqlServer", script);
    }

    [PostgreSqlFact]
    public async Task DedicatedPostgreSql_ShouldMigrateSeedReapplyAndEnforceSoftDelete()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION_STRING")!;
        await using var db = new EquilibraFitPlusPlusDbContext(
            new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>().UseNpgsql(connection).Options);
        await db.Database.MigrateAsync();
        var before = await db.Tenants.CountAsync();
        await db.Database.MigrateAsync();
        Assert.Equal(before, await db.Tenants.CountAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.True(await db.Tenants.AnyAsync(x => x.Id == SeedData.DefaultTenantId));
        Assert.True(await db.Database.SqlQueryRaw<bool>("""
            SELECT relrowsecurity AS "Value" FROM pg_class
            WHERE oid = 'public."__EFMigrationsHistory"'::regclass
            """).SingleAsync());
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("""
            SELECT COUNT(*)::integer AS "Value" FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE n.nspname = 'app' AND c.relkind = 'r' AND NOT c.relrowsecurity
            """).SingleAsync());
        await using var transaction = await db.Database.BeginTransactionAsync();
        await RelationalDatabaseTests.VerifySoftDeleteAsync(db);
        await transaction.RollbackAsync();
    }
}
