using System.Security.Cryptography;
using EquilibraFitPlusPlus.Application.Abstractions.Billing;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Infrastructure.Billing;
using EquilibraFitPlusPlus.Infrastructure.Data;
using EquilibraFitPlusPlus.Infrastructure.Data.Seed;
using EquilibraFitPlusPlus.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Billing;

public sealed class GooglePlayBillingTests
{
    [Theory]
    [InlineData("SUBSCRIPTION_STATE_ACTIVE", true)]
    [InlineData("SUBSCRIPTION_STATE_IN_GRACE_PERIOD", true)]
    [InlineData("SUBSCRIPTION_STATE_CANCELED", true)]
    [InlineData("SUBSCRIPTION_STATE_ON_HOLD", false)]
    [InlineData("SUBSCRIPTION_STATE_PAUSED", false)]
    [InlineData("SUBSCRIPTION_STATE_EXPIRED", false)]
    [InlineData("SUBSCRIPTION_STATE_PENDING", false)]
    [InlineData("SUBSCRIPTION_STATE_UNSPECIFIED", false)]
    public async Task VerifiedStates_ShouldControlServerEntitlement(string state, bool premium)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var db = new EquilibraFitPlusPlusDbContext(new DbContextOptionsBuilder<EquilibraFitPlusPlusDbContext>().UseSqlite(connection).Options);
        await db.Database.EnsureCreatedAsync();
        var user = new Usuario { TenantId = SeedData.DefaultTenantId, IdentityUserId = Guid.NewGuid(), Nome = "Billing", Email = "billing@example.test" };
        db.Usuarios.Add(user);
        await db.SaveChangesAsync();
        var provider = new ProviderStub(state);
        var protector = Protector();
        var billing = new GooglePlayBillingService(db, provider, protector, TimeProvider.System, NullLogger<GooglePlayBillingService>.Instance);
        var result = await billing.ProcessAsync(user.TenantId, user.Id, "test-receipt", "event-1", default);
        Assert.Equal(premium, result.Premium);
        Assert.Equal(BillingTokenProtector.AccountId(user.Id), provider.AccountId);
        await billing.ProcessAsync(user.TenantId, user.Id, "test-receipt", "event-1", default);
        Assert.Single(await db.Assinaturas.ToArrayAsync());
        Assert.Single(await db.BillingEvents.ToArrayAsync());
        Assert.Equal(premium ? 2 : 0, provider.Acknowledgements);
        var subscription = await db.Assinaturas.SingleAsync();
        Assert.NotEqual("test-receipt", subscription.PurchaseTokenEncrypted);
        Assert.Equal("test-receipt", protector.Decrypt(subscription.PurchaseTokenEncrypted!));
        Assert.Equal(premium, await new PremiumRepository(db).ObterAssinaturaAtivaAsync(user.TenantId, user.Id, default) != null);
        var other = new Usuario { TenantId = user.TenantId, IdentityUserId = Guid.NewGuid(), Nome = "Other", Email = "other@example.test" };
        db.Usuarios.Add(other);
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<BillingValidationException>(() => billing.ProcessAsync(other.TenantId, other.Id, "test-receipt", "event-2", default));
        Assert.Equal("billing.owner_mismatch", error.Code);
        subscription.ExpiracaoUtc = DateTimeOffset.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync();
        Assert.Null(await new PremiumRepository(db).ObterAssinaturaAtivaAsync(user.TenantId, user.Id, default));
    }

    [Fact]
    public void Encryption_ShouldDetectTamperingAndUseRandomNonces()
    {
        var protector = Protector();
        var first = protector.Encrypt("receipt");
        Assert.NotEqual(first, protector.Encrypt("receipt"));
        var bytes = Convert.FromBase64String(first);
        bytes[^1] ^= 1;
        Assert.ThrowsAny<CryptographicException>(() => protector.Decrypt(Convert.ToBase64String(bytes)));
    }

    private static BillingTokenProtector Protector() => new(new GooglePlayOptions { TokenEncryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) });
    private sealed class ProviderStub(string state) : IPaymentProvider
    {
        public string? AccountId { get; private set; }
        public int Acknowledgements { get; private set; }
        public Task<VerifiedSubscription> VerifyAsync(string token, string accountId, CancellationToken ct)
        {
            AccountId = accountId;
            return Task.FromResult(new VerifiedSubscription("premium.monthly", state, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddHours(1), true, true, false, "order-test", null));
        }
        public Task AcknowledgeAsync(string productId, string token, CancellationToken ct) { Acknowledgements++; return Task.CompletedTask; }
    }
}
