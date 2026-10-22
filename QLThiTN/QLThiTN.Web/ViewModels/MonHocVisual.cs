namespace QLThiTN.Web.ViewModels;

/// <summary>
/// Icon môn học, hỗ trợ cả tên có dấu và không dấu, và màu bìa đề tự sinh.
/// </summary>
public static class MonHocVisual
{
    public static string ToIcon(string? tenMon) => (tenMon ?? "").Trim() switch
    {
        "Toan" or "Toán" or "Toán học" => "bi-calculator",
        "Vat Ly" or "Vật lý" => "bi-lightning-charge",
        "Hoa Hoc" or "Hóa học" => "bi-eyedropper",
        "Sinh Hoc" or "Sinh học" => "bi-flower1",
        "Lich Su" or "Lịch sử" => "bi-hourglass-split",
        "Dia Ly" or "Địa lý" => "bi-globe-asia-australia",
        "Tin Hoc" or "Tin học" => "bi-pc-display",
        "Tieng Anh" or "Tiếng Anh" => "bi-translate",
        "Cong Nghe" or "Công nghệ" => "bi-gear-wide-connected",
        "Kinh Te Phap Luat" or "Kinh tế pháp luật" or "Kinh tế & Pháp luật" => "bi-bank",
        _ => "bi-book"
    };

    /// <summary>
    /// Hiển thị tên môn học chuẩn tiếng Việt có dấu.
    /// </summary>
    public static string ToDisplayName(string? tenMon) => (tenMon ?? "").Trim() switch
    {
        "Toan" or "Toán" or "Toán học" => "Toán",
        "Vat Ly" or "Vật lý" => "Vật lý",
        "Hoa Hoc" or "Hóa học" => "Hóa học",
        "Sinh Hoc" or "Sinh học" => "Sinh học",
        "Lich Su" or "Lịch sử" => "Lịch sử",
        "Dia Ly" or "Địa lý" => "Địa lý",
        "Tin Hoc" or "Tin học" => "Tin học",
        "Tieng Anh" or "Tiếng Anh" => "Tiếng Anh",
        "Cong Nghe" or "Công nghệ" => "Công nghệ",
        "Kinh Te Phap Luat" or "Kinh tế pháp luật" or "Kinh tế & Pháp luật" => "Kinh tế & Pháp luật",
        var other when !string.IsNullOrWhiteSpace(other) => other,
        _ => "Môn học"
    };

    /// <summary>Chi so palette pastel cho bia de — dao dong theo ID de
    /// de cac de ke nhau co mau khac nhau, on dinh theo thoi gian.</summary>
    public static int CoverPalette(int deThiId) => Math.Abs(deThiId) % 6;
}
