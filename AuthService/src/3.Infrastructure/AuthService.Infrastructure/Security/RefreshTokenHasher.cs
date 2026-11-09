using System.Security.Cryptography;
using System.Text;

namespace AuthService.Infrastructure.Security;

/// <summary>
/// Hashes refresh-token secrets before they are persisted.
/// Uses SHA-256 to produce a 64-char hex digest.
/// </summary>
public class RefreshTokenHasher
{
    public string Hash(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ArgumentException("rawToken must not be null or blank", nameof(rawToken));
        }

        byte[] bytes = Encoding.UTF8.GetBytes(rawToken);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
