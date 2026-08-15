using Application.Utilities.Extensions;

namespace Application.Features.Dbo.Users;

internal static class UserPasswordHelpers
{
    internal static bool MatchesStoredPassword(string storedPassword, string plainPassword)
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

        // Legacy plain-text rows (pre-encryption dev data) — Keeping this fallback intentionally.
        return string.Equals(storedPassword, plain, StringComparison.OrdinalIgnoreCase);
    }
}
