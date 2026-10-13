namespace QLThiTN.Web.ViewModels;

/// <summary>
/// Icon môn học, hỗ trợ cả tên có dấu và không dấu, và màu bìa đề tự sinh.
/// </summary>
public static class MonHocVisual
{
    public static string ToIcon(string? tenMon) => (tenMon ?? "").Trim() switch
    {
        "Toan" or "Toán" => "bi-calculator",
        "Vat Ly" or "Vật lý" => "bi-lightning-charge",
        "Hoa Hoc" or "Hóa học" => "bi-eyedropper",
        "Sinh Hoc" or "Sinh học" => "bi-flower1",
        "Lich Su" or "Lịch sử" => "bi-hourglass-split",
        "Dia Ly" or "Địa lý" => "bi-globe-asia-australia",
        "Tin Hoc" or "Tin học" => "bi-pc-display",
        "Tieng Anh" or "Tiếng Anh" => "bi-translate",
        "Cong Nghe" or "Công nghệ" => "bi-gear-wide-connected",
        "Kinh Te Phap Luat" or "Kinh tế pháp luật" => "bi-bank",
        _ => "bi-book"
    };

    /// <summary>Chi so palette pastel cho bia de — dao dong theo ID de
    /// de cac de ke nhau co mau khac nhau, on dinh theo thoi gian.</summary>
    public static int CoverPalette(int deThiId) => Math.Abs(deThiId) % 6;
}
