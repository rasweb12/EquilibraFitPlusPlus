using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Data;

/// <summary>
/// Tests the Entity Framework Core model configuration.
/// </summary>
public sealed class EquilibraFitPlusPlusDbContextModelTests
{
    /// <summary>
    /// Ensures refresh tokens are mapped with a unique token hash index.
    /// </summary>
    [Fact]
    public void Model_ShouldMapRefreshTokenHashAsUniqueIndex()
    {
        using var dbContext = CreateDbContext();
        var entityType = dbContext.Model.FindEntityType(typeof(RefreshToken));

        Assert.NotNull(entityType);
        Assert.Contains(entityType!.GetIndexes(), index =>
            index.IsUnique && index.Properties.Any(property => property.Name == nameof(RefreshToken.TokenHash)));
    }

    /// <summary>
    /// Ensures business users are isolated by tenant and email uniqueness.
    /// </summary>
    [Fact]
    public void Model_ShouldMapUsuarioTenantEmailAsUniqueIndex()
    {
        using var dbContext = CreateDbContext();
        var entityType = dbContext.Model.FindEntityType(typeof(Usuario));

        Assert.NotNull(entityType);
        Assert.Contains(entityType!.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Any(property => property.Name == nameof(Usuario.TenantId))
            && index.Properties.Any(property => property.Name == nameof(Usuario.Email)));
    }

    /// <summary>
    /// Ensures workout exercises are constrained to the exercise catalog.
    /// </summary>
    [Fact]
    public void Model_ShouldMapTreinoExercicioToExerciseCatalog()
    {
        using var dbContext = CreateDbContext();
        var entityType = dbContext.Model.FindEntityType(typeof(TreinoExercicio));

        Assert.NotNull(entityType);
        Assert.Contains(entityType!.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Any(property => property.Name == nameof(TreinoExercicio.ExercicioId))
            && foreignKey.PrincipalEntityType.ClrType == typeof(Exercicio));
    }

    /// <summary>
    /// Ensures AI Coach messages are owned by chat sessions.
    /// </summary>
    [Fact]
    public void Model_ShouldMapChatMessageToChatSession()
    {
        using var dbContext = CreateDbContext();
        var entityType = dbContext.Model.FindEntityType(typeof(ChatMessage));

        Assert.NotNull(entityType);
        Assert.Contains(entityType!.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Any(property => property.Name == nameof(ChatMessage.ChatSessionId))
            && foreignKey.PrincipalEntityType.ClrType == typeof(ChatSession));
    }

    /// <summary>
    /// Ensures feature flags are unique per tenant and key.
    /// </summary>
    [Fact]
    public void Model_ShouldMapFeatureFlagTenantKeyAsUniqueIndex()
    {
        using var dbContext = CreateDbContext();
        var entityType = dbContext.Model.FindEntityType(typeof(FeatureFlag));

        Assert.NotNull(entityType);
        Assert.Contains(entityType!.GetIndexes(), index =>
            index.IsUnique
            && index.Properties.Any(property => property.Name == nameof(FeatureFlag.TenantId))
            && index.Properties.Any(property => property.Name == nameof(FeatureFlag.Chave)));
    }

    private static EquilibraFitPlusPlusDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new EquilibraFitPlusPlusDbContext(options);
    }
}
