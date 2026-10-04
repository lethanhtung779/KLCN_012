using System.Security.Cryptography;
using System.Text;

namespace QLThiTN.WinForms.Services;

/// <summary>
/// Bam mat khau giong het voi PasswordHelper cua QLThiTN.API:
/// SHA-256 tren ma hoa UTF-16LE, xuat chuoi hex hoa — hai phan he
/// dung chung bang TaiKhoan nen thuat toan phai khop tuyet doi.
/// </summary>
public static class PasswordHelper
{
    public static string Hash(string plain)
    {
        var bytes = SHA256.HashData(Encoding.Unicode.GetBytes(plain));
        return Convert.ToHexString(bytes);
    }

    public static bool Verify(string plain, string stored)
    {
        if (string.IsNullOrEmpty(stored)) return false;
        return string.Equals(Hash(plain), stored, StringComparison.OrdinalIgnoreCase);
    }
}
