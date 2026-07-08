using System.Security.Cryptography;
using System.Text;

namespace CreditReporting.Api.Services;

/// <summary>Masking and hashing helpers for SSNs, account numbers, and passwords.</summary>
public static class Masking
{
    public static string MaskSsn(string last4) => $"***-**-{last4}";

    public static string MaskAccountNumber(string accountNumber) =>
        accountNumber.Length <= 4
            ? new string('*', accountNumber.Length)
            : $"****{accountNumber[^4..]}";

    /// <summary>SHA-256 hash of the raw SSN, used instead of storing the real value.</summary>
    public static string HashSsn(string rawSsn)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawSsn));
        return Convert.ToHexString(bytes);
    }

    public static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToHexString(salt)}.{Convert.ToHexString(hash)}";
    }

    public static bool VerifyPassword(string password, string stored)
    {
        var parts = stored.Split('.');
        if (parts.Length != 2) return false;
        byte[] salt = Convert.FromHexString(parts[0]);
        byte[] expected = Convert.FromHexString(parts[1]);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
