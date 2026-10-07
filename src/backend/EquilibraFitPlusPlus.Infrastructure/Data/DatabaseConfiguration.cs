using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.Data;

/// <summary>Explicit provider selection shared by runtime and tooling.</summary>
public static class DatabaseConfiguration
{
    public static string ResolveConnectionString(string? supabase, string? defaultConnection) =>
        !string.IsNullOrWhiteSpace(supabase) ? supabase :
        !string.IsNullOrWhiteSpace(defaultConnection) ? defaultConnection :
        throw new InvalidOperationException("Configure SUPABASE_DB_CONNECTION_STRING or ConnectionStrings__Default.");

    /// <summary>Configures managed PostgreSQL or local development SQLite.</summary>
    public static void Configure(DbContextOptionsBuilder options, string provider, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Configure SUPABASE_DB_CONNECTION_STRING or ConnectionStrings__Default.");
        }

        if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            options.UseSqlite(connectionString);
            return;
        }

        if (!provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("DATABASE_PROVIDER must be PostgreSQL or Sqlite.");
        }

        options.UseNpgsql(connectionString, postgres =>
        {
            postgres.MigrationsAssembly(typeof(EquilibraFitPlusPlusDbContext).Assembly.FullName);
            postgres.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
        });
    }
}
