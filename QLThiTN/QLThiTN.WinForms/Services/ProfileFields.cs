namespace QLThiTN.WinForms.Services;

public static class ProfileFields
{
    public static readonly (string Key, string Caption)[] Common =
    [
        ("DiaChi", "Địa chỉ liên hệ"), ("GioiTinh", "Giới tính"),
        ("TinhTrangCha", "Tình trạng cha"), ("TinhTrangMe", "Tình trạng mẹ"),
        ("HoTenCha", "Họ tên cha"), ("HoTenMe", "Họ tên mẹ"),
        ("SdtCha", "Số điện thoại cha"), ("SdtMe", "Số điện thoại mẹ"),
        ("NguoiLienHe", "Người liên hệ khẩn cấp"), ("QuanHeLienHe", "Quan hệ với người liên hệ"),
        ("SdtLienHe", "Số điện thoại liên hệ khẩn cấp"), ("GhiChu", "Ghi chú hồ sơ")
    ];

    public static readonly (string Key, string Caption)[] TeacherProfessional =
    [
        ("TrinhDoHocVan", "Trình độ học vấn"), ("BangCap", "Bằng cấp / chứng chỉ"),
        ("TruongHoc", "Trường đã học / tốt nghiệp"), ("ChuyenNganh", "Chuyên ngành đào tạo"),
        ("DonViCongTac", "Trường / đơn vị đang công tác")
    ];

    public static (string Key, string Caption)[] ForRole(string role) => role switch
    {
        "HocVien" => Common,
        "GiaoVien" => Common.Concat(TeacherProfessional).ToArray(),
        _ => throw new ArgumentException("Vai trò không hợp lệ.", nameof(role))
    };

    public static (string Key, string Caption)[] RequiredForRole(string role) =>
        ForRole(role).Where(field => IsAlwaysRequired(field.Key)).ToArray();

    public static bool IsAlwaysRequired(string key) => key is not
        ("GhiChu" or "HoTenCha" or "SdtCha" or "HoTenMe" or "SdtMe");
}
