using System.Net.Mail;

namespace QLThiTN.WinForms.Services;

public static class DirectoryRules
{
    public static string AccountCode(int id) => $"TK{id:D6}";
    public static string ClassCode(int id) => $"LH{id:D6}";

    public static int SearchId(string? text, string prefix)
    {
        var value = text?.Trim() ?? "";
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) value = value[prefix.Length..];
        return int.TryParse(value, out var id) && id > 0 ? id : 0;
    }

    public static bool ValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 100 || email.Any(char.IsWhiteSpace) ||
            !MailAddress.TryCreate(email, out var address) || address.Address != email) return false;
        var labels = address.Host.Split('.');
        return labels.Length >= 2 && labels.All(label => label.Length is > 0 and <= 63 &&
            char.IsAsciiLetterOrDigit(label[0]) && char.IsAsciiLetterOrDigit(label[^1]) &&
            label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')) &&
            labels[^1].Length >= 2 && labels[^1].All(char.IsAsciiLetter);
    }

    public static string Role(string? code) => code switch
    {
        "HocVien" => "Học viên", "GiaoVien" => "Giáo viên", "QuanTriVien" => "Quản trị viên", _ => code ?? ""
    };
    public static string Status(string? code) => code switch
    {
        "HoatDong" => "Hoạt động", "Khoa" or "BiKhoa" => "Đã khóa",
        "DangHoc" => "Đang học", "DaNghi" => "Đã nghỉ", "HoanThanh" => "Hoàn thành", _ => code ?? ""
    };
    public sealed record RoleOption(string Code)
    {
        public override string ToString() => Role(Code);
    }
}
