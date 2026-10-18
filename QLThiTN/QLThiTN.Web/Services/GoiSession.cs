namespace QLThiTN.Web.Services;

/// <summary>Goi tai khoan hoc vien luu trong session (MienPhi / DongPhi).</summary>
public static class GoiSession
{
    public const string Key = "LoaiGoi";

    public static bool LaDongPhi(HttpContext http) =>
        http.Session.GetString(Key) == "DongPhi";

    public static void Luu(ISession session, string? loaiGoi) =>
        session.SetString(Key, loaiGoi == "DongPhi" ? "DongPhi" : "MienPhi");

    public static async Task RefreshAsync(HttpContext http, ApiClient api)
    {
        var userId = http.Session.GetInt32("UserId");
        if (userId is not > 0)
            return;

        var profile = await api.GetProfileAsync(userId.Value);
        if (profile is null)
            return;

        if (string.IsNullOrEmpty(profile.LoaiGoi) && profile.HocVienID is null)
            return;

        Luu(http.Session, profile.LoaiGoi);
    }
}
