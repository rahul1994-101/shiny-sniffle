using Application.Utilities.Helpers;

namespace Application.Utilities.Extensions;

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
}
