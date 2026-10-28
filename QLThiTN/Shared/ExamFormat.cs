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
    public void Validate(IEnumerable<string> types)
    {
        var list = types.ToList();
        if (list.Count(t => t=="TracNghiem") != MultipleChoice || list.Count(t=>t=="DungSai") != TrueFalse ||
            list.Count(t=>t=="TraLoiNgan") != ShortAnswer || list.Count != MultipleChoice+TrueFalse+ShortAnswer)
            throw new InvalidOperationException($"Sai cấu trúc môn: cần {MultipleChoice} trắc nghiệm, {TrueFalse} đúng/sai, {ShortAnswer} trả lời ngắn. Không tự thêm/bớt câu khi trộn.");
    }
}
