using EquilibraFitPlusPlus.Infrastructure.Billing;
using Microsoft.Extensions.Configuration;

namespace EquilibraFitPlusPlus.Infrastructure.IntegrationTests.Billing;

public sealed class GooglePlayOptionsTests
{
    [Theory]
    [InlineData("GOOGLE_PLAY_PACKAGE_NAME")]
    [InlineData("GOOGLE_PLAY_PRODUCT_IDS")]
    [InlineData("BILLING_TOKEN_ENCRYPTION_KEY")]
    [InlineData("GOOGLE_PLAY_PUBSUB_AUDIENCE")]
    [InlineData("GOOGLE_PLAY_PUBSUB_SERVICE_ACCOUNT_EMAIL")]
    public void EnabledBilling_RequiresCompleteConfiguration(string missing)
    {
        var values = Values();
        values[missing] = "";
        var error = Assert.Throws<InvalidOperationException>(() => Read(values));
        Assert.Contains(missing, error.Message);
    }

    [Theory]
    [InlineData("not-base64-private-fixture")]
    [InlineData("c2hvcnQ=")]
    public void InvalidEncryptionKey_IsRejectedWithoutRevealingValue(string value)
    {
        var values = Values();
        values["BILLING_TOKEN_ENCRYPTION_KEY"] = value;
        var error = Assert.Throws<InvalidOperationException>(() => Read(values));
        Assert.Contains("BILLING_TOKEN_ENCRYPTION_KEY", error.Message);
        Assert.DoesNotContain(value, error.ToString());
    }

    [Theory]
    [InlineData("http://example.test/notifications")]
    [InlineData("https://user:private@example.test/notifications")]
    [InlineData("not-a-url")]
    public void InvalidPushAudience_IsRejected(string audience)
    {
        var values = Values();
        values["GOOGLE_PLAY_PUBSUB_AUDIENCE"] = audience;
        Assert.Throws<InvalidOperationException>(() => Read(values));
    }

    [Fact]
    public void DisabledBilling_DoesNotRequireExternalSecrets()
    {
        Assert.False(Read(new Dictionary<string, string?> { ["GooglePlay:Enabled"] = "false" }).Enabled);
    }

    [Fact]
    public void CompleteConfiguration_IsEnabledAndTrimsProducts()
    {
        var options = Read(Values());
        Assert.True(options.Enabled);
        Assert.Equal("br.com.equilibrafit.app.plusplus", options.PackageName);
        Assert.Equal(["test.monthly", "test.yearly"], options.ProductIds);
    }

    private static GooglePlayOptions Read(Dictionary<string, string?> values) =>
        GooglePlayOptions.FromConfiguration(new ConfigurationBuilder().AddInMemoryCollection(values).Build());

    private static Dictionary<string, string?> Values() => new()
    {
        ["GooglePlay:Enabled"] = "true",
        ["GOOGLE_PLAY_PACKAGE_NAME"] = "br.com.equilibrafit.app.plusplus",
        ["GOOGLE_PLAY_PRODUCT_IDS"] = " test.monthly, ,test.yearly ",
        ["BILLING_TOKEN_ENCRYPTION_KEY"] = Convert.ToBase64String(new byte[32]),
        ["GOOGLE_PLAY_PUBSUB_AUDIENCE"] = "https://example.test/api/v1/billing/google-play/notifications",
        ["GOOGLE_PLAY_PUBSUB_SERVICE_ACCOUNT_EMAIL"] = "push@example.test"
    };
}
