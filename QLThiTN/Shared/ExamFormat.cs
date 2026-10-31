using System.Globalization;
using System.Text;

namespace QLThiTN.Shared;

public record ExamFormat(int MultipleChoice, int TrueFalse, int ShortAnswer, int Minutes)
{
    // Quyết định 764/QĐ-BGDĐT, cấu trúc từ 2025. Tin học cần cấu trúc nhánh riêng.
    public static ExamFormat ForSubject(string subject)
    {
        var name = string.Concat(subject.Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)).ToLowerInvariant().Replace('đ','d');
        if (name.Contains("toan")) return new(12,4,6,90);
        if (name.Contains("tieng") || name.Contains("ngoai ngu")) return new(40,0,0,50);
        if (name.Contains("tin hoc"))
            return new(24,4,0,50); // A paper for one selected track: 2 common + 2 track questions.
        if (name.Contains("lich su") || name.Contains("kinh te") || name.Contains("cong nghe")) return new(24,4,0,50);
        if (name.Contains("vat") || name.Contains("hoa") || name.Contains("sinh") || name.Contains("dia")) return new(18,4,6,50);
        throw new InvalidOperationException("Môn này chưa có mẫu đề trắc nghiệm THPT được hỗ trợ.");
    }

    public string? GetValidationError(IEnumerable<string> types)
    {
        var list = types.ToList();
        var actualMultipleChoice = list.Count(t => t == "TracNghiem");
        var actualTrueFalse = list.Count(t => t == "DungSai");
        var actualShortAnswer = list.Count(t => t == "TraLoiNgan");
        var unknown = list.Count - actualMultipleChoice - actualTrueFalse - actualShortAnswer;

        if (actualMultipleChoice == MultipleChoice && actualTrueFalse == TrueFalse &&
            actualShortAnswer == ShortAnswer && unknown == 0)
            return null;

        var expectedTotal = MultipleChoice + TrueFalse + ShortAnswer;
        var details = $"Mẫu THPT cần {expectedTotal} câu: {MultipleChoice} trắc nghiệm, " +
                      $"{TrueFalse} đúng/sai, {ShortAnswer} trả lời ngắn. " +
                      $"Đề hiện có {list.Count} câu: {actualMultipleChoice} trắc nghiệm, " +
                      $"{actualTrueFalse} đúng/sai, {actualShortAnswer} trả lời ngắn.";
        if (unknown > 0)
            details += $" Có {unknown} câu chưa xác định đúng loại.";
        return details + " Không tự thêm hoặc bớt câu khi trộn.";
    }

    public void Validate(IEnumerable<string> types)
    {
        var error = GetValidationError(types);
        if (error != null)
            throw new InvalidOperationException(error);
    }
}
