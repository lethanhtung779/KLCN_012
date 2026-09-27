using System.Security.Cryptography;
using System.Text;

namespace QLThiTN.API.Common;

public static class PasswordHelper
{
    public static string Hash(string plain)
        => Convert.ToHexString(SHA256.HashData(Encoding.Unicode.GetBytes(plain)));

    public static bool Verify(string plain, string storedHash)
        => !string.IsNullOrEmpty(storedHash) && Hash(plain).Equals(storedHash, StringComparison.OrdinalIgnoreCase);
}