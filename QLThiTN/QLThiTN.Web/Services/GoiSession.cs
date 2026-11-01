namespace QLThiTN.Web.Services;

/// <summary>Quyền VIP hiệu lực: VIP cá nhân hoặc học viên trung tâm đang thuộc lớp.</summary>
public static class GoiSession
{
    public const string Key = "LoaiGoi";
    public const string VipKey = "LaVip";
    public const string CenterStudentKey = "LaHocVienTrungTam";
    public const string VipSourceKey = "NguonVip";

    public static bool LaDongPhi(HttpContext http) =>
        http.Session.GetString(VipKey) == "1" || http.Session.GetString(Key) == "DongPhi";

    public static void Luu(ISession session, string? loaiGoi, bool? laVip = null,
        bool laHocVienTrungTam = false, string? nguonVip = null)
    {
        var vip = laVip ?? loaiGoi == "DongPhi";
        session.SetString(Key, vip ? "DongPhi" : "MienPhi");
        session.SetString(VipKey, vip ? "1" : "0");
        session.SetString(CenterStudentKey, laHocVienTrungTam ? "1" : "0");
        session.SetString(VipSourceKey, nguonVip ?? (vip ? "CaNhan" : "KhongCo"));
    }

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

        Luu(http.Session, profile.LoaiGoi, profile.LaVip, profile.LaHocVienTrungTam, profile.NguonVip);
    }
}
