using WebApp.Utilities.Helpers;

namespace WebApp.Utilities.Extensions;

public static class CryptographyExtensions
{
    public static string Encrypt(this string plainText)
    {
        var cipherText = CryptographyHelpers.Encrypt(plainText);
        return cipherText;
    }

    public static string Decrypt(this string cipherText)
    {
        var plainText = CryptographyHelpers.Decrypt(cipherText);
        return plainText;
    }

    public static bool MatchesStoredPassword(this string storedPassword, string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword) || string.IsNullOrWhiteSpace(plainPassword))
        {
            return false;
        }

        var plain = plainPassword.Trim();

        if (string.Equals(storedPassword, plain.Encrypt(), StringComparison.Ordinal))
        {
            return true;
        }

        // Legacy plain-text rows (pre-encryption dev data) — removed on next password change.
        return string.Equals(storedPassword, plain, StringComparison.OrdinalIgnoreCase);
    }
}
