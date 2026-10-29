namespace QLThiTN.WinForms.Services;

public static class ProfileFields
{
    public static readonly (string Key, string Caption)[] Basic =
    [
        ("DiaChi", "Địa chỉ liên hệ"), ("GioiTinh", "Giới tính"),
    ];

    public static readonly (string Key, string Caption)[] StudentParents =
    [
        ("TinhTrangCha", "Tình trạng cha"), ("TinhTrangMe", "Tình trạng mẹ"),
        ("HoTenCha", "Họ tên cha"), ("HoTenMe", "Họ tên mẹ"),
        ("SdtCha", "Số điện thoại cha"), ("SdtMe", "Số điện thoại mẹ")
    ];

    public static readonly (string Key, string Caption)[] EmergencyContact =
    [
        ("NguoiLienHe", "Người liên hệ khẩn cấp"), ("QuanHeLienHe", "Quan hệ với người liên hệ"),
        ("SdtLienHe", "Số điện thoại liên hệ khẩn cấp"), ("GhiChu", "Ghi chú hồ sơ")
    ];

    public static readonly (string Key, string Caption)[] StudentGuardian =
    [
        ("HoTenNguoiGiamHo", "Họ tên người giám hộ"),
        ("QuanHeNguoiGiamHo", "Quan hệ với học viên"),
        ("SdtNguoiGiamHo", "Số điện thoại người giám hộ")
    ];

    public static readonly (string Key, string Caption)[] TeacherProfessional =
    [
        ("TrinhDoHocVan", "Trình độ học vấn"), ("BangCap", "Bằng cấp / chứng chỉ"),
        ("TruongHoc", "Trường đã học / tốt nghiệp"), ("ChuyenNganh", "Chuyên ngành đào tạo"),
        ("DonViCongTac", "Trường / đơn vị đang công tác")
    ];

    public static (string Key, string Caption)[] ForRole(string role) => role switch
    {
        // Thông tin cha/mẹ chỉ phục vụ hồ sơ học viên. Giáo viên chỉ cần
        // thông tin cá nhân, liên hệ khẩn cấp và hồ sơ chuyên môn.
        "HocVien" => Basic.Concat(StudentParents).Concat(EmergencyContact).Concat(StudentGuardian).ToArray(),
        "GiaoVien" => Basic.Concat(EmergencyContact).Concat(TeacherProfessional).ToArray(),
        _ => throw new ArgumentException("Vai trò không hợp lệ.", nameof(role))
    };

    public static (string Key, string Caption)[] RequiredForRole(string role) =>
        ForRole(role).Where(field => IsAlwaysRequired(field.Key)).ToArray();

    public static bool IsAlwaysRequired(string key) => key is not
        ("GhiChu" or "HoTenNguoiGiamHo" or "QuanHeNguoiGiamHo" or "SdtNguoiGiamHo" or
         "TinhTrangCha" or "TinhTrangMe" or
         "HoTenCha" or "SdtCha" or "HoTenMe" or "SdtMe");
}
