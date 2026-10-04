namespace QLThiTN.Web.ViewModels;

/// <summary>
/// Icon Bootstrap Icons cho tung mon hoc (TenMon trong DB dang khong dau:
/// Toan, Vat Ly, Hoa Hoc...) va palette pastel cho bia de tu sinh.
/// </summary>
public static class MonHocVisual
{
    public static string ToIcon(string? tenMon) => (tenMon ?? "").Trim() switch
    {
        "Toan" => "bi-calculator",
        "Vat Ly" => "bi-lightning-charge",
        "Hoa Hoc" => "bi-eyedropper",
        "Sinh Hoc" => "bi-flower1",
        "Lich Su" => "bi-hourglass-split",
        "Dia Ly" => "bi-globe-asia-australia",
        "Tin Hoc" => "bi-pc-display",
        "Tieng Anh" => "bi-translate",
        "Cong Nghe" => "bi-gear-wide-connected",
        "Kinh Te Phap Luat" => "bi-bank",
        _ => "bi-book"
    };

    /// <summary>Chi so palette pastel cho bia de — dao dong theo ID de
    /// de cac de ke nhau co mau khac nhau, on dinh theo thoi gian.</summary>
    public static int CoverPalette(int deThiId) => Math.Abs(deThiId) % 6;
}
