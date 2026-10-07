using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Data;

public sealed class SupabaseFactAttribute : FactAttribute
{
    public SupabaseFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_SUPABASE_DB_CONNECTION_STRING")))
            Skip = "Configure TEST_SUPABASE_DB_CONNECTION_STRING for a dedicated Supabase test project.";
    }
}

public sealed class SupabaseRlsTests
{
    [SupabaseFact]
    public async Task AuthenticatedRole_ShouldReadOnlyOwnDataAndDenyAllClientWritesAndBillingReads()
    {
        await using var db = new EquilibraFitPlusPlusDbContext(new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TEST_SUPABASE_DB_CONNECTION_STRING")!).Options);
        await db.Database.MigrateAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        var tenant = new Tenant { Nome = "RLS test", Tipo = "Individual" };
        var a = new Usuario { TenantId = tenant.Id, Nome = "A", Email = $"{Guid.NewGuid():N}@example.test", IdentityUserId = Guid.NewGuid() };
        var b = new Usuario { TenantId = tenant.Id, Nome = "B", Email = $"{Guid.NewGuid():N}@example.test", IdentityUserId = Guid.NewGuid() };
        var wa = Workout(a);
        var wb = Workout(b);
        db.AddRange(tenant, a, b, wa, wb);
        await db.SaveChangesAsync();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await Command("SET LOCAL ROLE authenticated");
        await Subject(a.IdentityUserId);
        Assert.Equal(1L, await Scalar("SELECT COUNT(*) FROM app.\"TreinosUsuario\""));
        Assert.Equal(wa.Id, await Scalar("SELECT \"Id\" FROM app.\"TreinosUsuario\""));
        Assert.Equal(a.Id, await Scalar("SELECT \"Id\" FROM app.\"Usuarios\""));
        foreach (var sql in new[]
        {
            "INSERT INTO app.\"TreinosUsuario\" DEFAULT VALUES",
            "UPDATE app.\"TreinosUsuario\" SET \"Nome\" = 'forbidden'",
            "DELETE FROM app.\"TreinosUsuario\"",
            "SELECT * FROM app.\"Assinaturas\"",
            "SELECT * FROM app.\"Pagamentos\"",
            "SELECT * FROM app.\"BillingEvents\"",
            "SELECT * FROM public.\"__EFMigrationsHistory\""
        })
        {
            await tx.CreateSavepointAsync("denied");
            var error = await Assert.ThrowsAsync<PostgresException>(() => Command(sql));
            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
            await tx.RollbackToSavepointAsync("denied");
        }
        await Subject(b.IdentityUserId);
        Assert.Equal(1L, await Scalar("SELECT COUNT(*) FROM app.\"TreinosUsuario\""));
        Assert.Equal(wb.Id, await Scalar("SELECT \"Id\" FROM app.\"TreinosUsuario\""));
        await tx.RollbackAsync();

        async Task Command(string sql)
        {
            await using var cmd = new NpgsqlCommand(sql, connection);
            await cmd.ExecuteNonQueryAsync();
        }
        async Task<object?> Scalar(string sql)
        {
            await using var cmd = new NpgsqlCommand(sql, connection);
            return await cmd.ExecuteScalarAsync();
        }
        async Task Subject(Guid subject)
        {
            await using var cmd = new NpgsqlCommand("SELECT set_config('request.jwt.claim.sub', @subject, true), set_config('request.jwt.claims', @claims, true)", connection);
            cmd.Parameters.AddWithValue("subject", subject.ToString());
            cmd.Parameters.AddWithValue("claims", System.Text.Json.JsonSerializer.Serialize(new { sub = subject, role = "authenticated" }));
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static TreinoUsuario Workout(Usuario user) => new()
    {
        TenantId = user.TenantId, UsuarioId = user.Id, Nome = "Private workout", Objetivo = "Hipertrofia",
        FrequenciaSemanal = 3, Versao = 1, DataInicio = new DateOnly(2026, 10, 1), DuracaoSemanas = 6, Fase = "Base", Ativo = true
    };
}
