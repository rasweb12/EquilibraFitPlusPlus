using System.Security.Cryptography;
using System.Text;

namespace EquilibraFitPlusPlus.Infrastructure.Authentication;

/// <summary>
/// Generates and hashes refresh tokens.
/// </summary>
public static class RefreshTokenGenerator
{
    /// <summary>
    /// Creates a cryptographically strong token.
    /// </summary>
    public static string GenerateToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    /// <summary>
    /// Hashes a token before persistence.
    /// </summary>
    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
