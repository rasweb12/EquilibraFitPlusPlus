using Microsoft.Extensions.Configuration;

namespace EquilibraFitPlusPlus.Infrastructure.Authentication;

/// <summary>Public Auth endpoint configuration. Secrets belong to the trusted backend.</summary>
public sealed record SupabaseOptions(Uri Url, string PublishableKey)
{
    public static SupabaseOptions FromConfiguration(IConfiguration configuration)
    {
        string? url = configuration["SUPABASE_URL"] ?? configuration["Supabase:Url"];
        string? key = configuration["SUPABASE_PUBLISHABLE_KEY"] ?? configuration["Supabase:PublishableKey"];
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Configure HTTPS SUPABASE_URL and SUPABASE_PUBLISHABLE_KEY.");
        }
        return new SupabaseOptions(new Uri(uri.GetLeftPart(UriPartial.Authority) + "/"), key);
    }
}
