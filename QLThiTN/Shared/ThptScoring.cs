using System.Globalization;
using System.Text;

namespace QLThiTN.Shared;

/// <summary>Điểm chuẩn theo cấu trúc đề tốt nghiệp THPT từ năm 2025.</summary>
public static class ThptScoring
{
    public static decimal MaxPoint(string subject, string questionType) => questionType switch
    {
        "DungSai" => 1.00m,
        "TraLoiNgan" => IsMath(subject) ? 0.50m : 0.25m,
        _ => 0.25m
    };

    public static decimal TrueFalsePoint(int correctStatements) => correctStatements switch
    {
        1 => 0.10m,
        2 => 0.25m,
        3 => 0.50m,
        4 => 1.00m,
        _ => 0m
    };

    public static bool IsMath(string subject)
    {
        var normalized = string.Concat(subject.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
            .Replace('đ', 'd').Replace('Đ', 'D');
        return normalized.Contains("Toan", StringComparison.OrdinalIgnoreCase);
    }
}
