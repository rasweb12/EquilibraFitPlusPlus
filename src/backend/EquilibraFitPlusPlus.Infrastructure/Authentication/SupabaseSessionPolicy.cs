using System.Globalization;
using System.Security.Claims;
using System.Text.Json;

namespace EquilibraFitPlusPlus.Infrastructure.Authentication;

/// <summary>Global revocation policy for claims already trusted by JWT validation or the Auth HTTPS response.</summary>
public static class SupabaseSessionPolicy
{
    /// <summary>Refresh must not turn an old authentication into a new sign-in after revocation.</summary>
    public static bool IsRevoked(IEnumerable<Claim> claims, DateTimeOffset cutoff)
    {
        var all = claims.ToArray();
        if (!long.TryParse(all.FirstOrDefault(x => x.Type == "iat")?.Value,
            NumberStyles.Integer, CultureInfo.InvariantCulture, out var issued) || issued <= cutoff.ToUnixTimeSeconds()) return true;
        long? authenticated = null;
        try
        {
            foreach (var claim in all.Where(x => x.Type == "amr"))
            {
                using var document = JsonDocument.Parse(claim.Value);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Array)
                    foreach (var item in root.EnumerateArray()) Inspect(item);
                else Inspect(root);
            }
        }
        catch (JsonException) { return true; }
        return authenticated is null || authenticated <= cutoff.ToUnixTimeSeconds();

        void Inspect(JsonElement item)
        {
            if (item.ValueKind != JsonValueKind.Object || !item.TryGetProperty("method", out var method) ||
                method.ValueKind != JsonValueKind.String || method.GetString() is not
                ("password" or "oauth" or "otp" or "magiclink" or "email/signup" or "recovery" or "invite" or "sso/saml")) return;
            if (item.TryGetProperty("timestamp", out var timestamp) && timestamp.ValueKind == JsonValueKind.Number && timestamp.TryGetInt64(out var value))
                authenticated = authenticated is null ? value : Math.Min(authenticated.Value, value);
        }
    }
}
