using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EquilibraFitPlusPlus.Infrastructure.Data;

/// <summary>
/// Design-time factory used by Entity Framework migrations.
/// </summary>
public sealed class EquilibraFitPlusPlusDbContextFactory : IDesignTimeDbContextFactory<EquilibraFitPlusPlusDbContext>
{
    /// <inheritdoc />
    public EquilibraFitPlusPlusDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>();
        string provider = Environment.GetEnvironmentVariable("DATABASE_PROVIDER")
            ?? Environment.GetEnvironmentVariable("Database__Provider") ?? "PostgreSQL";
        string connection = DatabaseConfiguration.ResolveConnectionString(
            Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION_STRING"),
            Environment.GetEnvironmentVariable("ConnectionStrings__Default"));
        DatabaseConfiguration.Configure(optionsBuilder, provider, connection);
        return new EquilibraFitPlusPlusDbContext(optionsBuilder.Options);
    }
}
