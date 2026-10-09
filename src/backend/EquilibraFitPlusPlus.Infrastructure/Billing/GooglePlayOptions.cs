using Microsoft.Extensions.Configuration;

namespace EquilibraFitPlusPlus.Infrastructure.Billing;

public sealed class GooglePlayOptions
{
    public bool Enabled { get; init; }
    public string PackageName { get; init; } = string.Empty;
    public string[] ProductIds { get; init; } = [];
    public string TokenEncryptionKey { get; init; } = string.Empty;
    public string PubSubAudience { get; init; } = string.Empty;
    public string PubSubServiceAccountEmail { get; init; } = string.Empty;

    public static GooglePlayOptions FromConfiguration(IConfiguration config)
    {
        var options = new GooglePlayOptions
        {
            Enabled = bool.TryParse(config["GooglePlay:Enabled"], out var enabled) && enabled,
            PackageName = config["GOOGLE_PLAY_PACKAGE_NAME"] ?? config["GooglePlay:PackageName"] ?? string.Empty,
            ProductIds = (config["GOOGLE_PLAY_PRODUCT_IDS"] ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            TokenEncryptionKey = config["BILLING_TOKEN_ENCRYPTION_KEY"] ?? string.Empty,
            PubSubAudience = config["GOOGLE_PLAY_PUBSUB_AUDIENCE"] ?? string.Empty,
            PubSubServiceAccountEmail = config["GOOGLE_PLAY_PUBSUB_SERVICE_ACCOUNT_EMAIL"] ?? string.Empty
        };
        options.Validate();
        return options;
    }

    private void Validate()
    {
        if (!Enabled) return;
        if (string.IsNullOrWhiteSpace(PackageName))
            throw new InvalidOperationException("Configure GOOGLE_PLAY_PACKAGE_NAME before enabling billing.");
        if (ProductIds.Length == 0)
            throw new InvalidOperationException("Configure GOOGLE_PLAY_PRODUCT_IDS before enabling billing.");
        byte[] key;
        try { key = Convert.FromBase64String(TokenEncryptionKey); }
        catch (FormatException)
        {
            throw new InvalidOperationException("Configure BILLING_TOKEN_ENCRYPTION_KEY as 32 random bytes encoded in Base64.");
        }
        if (key.Length != 32)
            throw new InvalidOperationException("Configure BILLING_TOKEN_ENCRYPTION_KEY as 32 random bytes encoded in Base64.");
        if (!Uri.TryCreate(PubSubAudience, UriKind.Absolute, out var audience) ||
            audience.Scheme != Uri.UriSchemeHttps || !string.IsNullOrEmpty(audience.UserInfo))
            throw new InvalidOperationException("Configure GOOGLE_PLAY_PUBSUB_AUDIENCE with the HTTPS notifications endpoint.");
        if (string.IsNullOrWhiteSpace(PubSubServiceAccountEmail))
            throw new InvalidOperationException("Configure GOOGLE_PLAY_PUBSUB_SERVICE_ACCOUNT_EMAIL before enabling billing.");
    }
}
