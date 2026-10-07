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

    public static GooglePlayOptions FromConfiguration(IConfiguration config) => new()
    {
        Enabled = bool.TryParse(config["GooglePlay:Enabled"], out var enabled) && enabled,
        PackageName = config["GOOGLE_PLAY_PACKAGE_NAME"] ?? config["GooglePlay:PackageName"] ?? string.Empty,
        ProductIds = (config["GOOGLE_PLAY_PRODUCT_IDS"] ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        TokenEncryptionKey = config["BILLING_TOKEN_ENCRYPTION_KEY"] ?? string.Empty,
        PubSubAudience = config["GOOGLE_PLAY_PUBSUB_AUDIENCE"] ?? string.Empty,
        PubSubServiceAccountEmail = config["GOOGLE_PLAY_PUBSUB_SERVICE_ACCOUNT_EMAIL"] ?? string.Empty
    };
}
