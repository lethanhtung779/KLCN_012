using System.Text.Json;
using QLThiTN.Shared;
using QLThiTN.WinForms.Models;
using QLThiTN.WinForms.Services;

const string Prefix = "pct_";
var config = Path.Combine(AppContext.BaseDirectory, "test-config");
Directory.CreateDirectory(config);
var database = "ThiTracNghiem_WorkflowTest20261028";
File.WriteAllText(Path.Combine(config, "appsettings.json"), JsonSerializer.Serialize(new
{
    ConnectionStrings = new
    {
        ThiTracNghiem = $"Server=.\\SQLEXPRESS;Database={database};Trusted_Connection=True;TrustServerCertificate=True"
    }
}));
Database.LoadConfig(config);
Database.Execute(WorkflowSchema.Sql);

var admin = Database.ExecuteScalarInt("SELECT TOP 1 TaiKhoanID FROM TaiKhoan WHERE VaiTro='QuanTriVien' AND TrangThai='HoatDong'");
if (admin == 0) throw new InvalidOperationException("Database test không có tài khoản quản trị đang hoạt động.");
Session.Set(new TaiKhoan { TaiKhoanID = admin, VaiTro = "QuanTriVien" });
var subjectId = Database.ExecuteScalarInt("SELECT TOP 1 MonHocID FROM MonHoc ORDER BY MonHocID");
if (subjectId == 0) throw new InvalidOperationException("Database test không có môn học.");

var service = new PersonManagementService();
var passed = 0;
var failed = new List<string>();
var sequence = 0;

void Cleanup()
{
    Database.Execute(@"
        DELETE gm FROM GiaoVien_MonHoc gm JOIN GiaoVien gv ON gv.GiaoVienID=gm.GiaoVienID
          JOIN TaiKhoan tk ON tk.TaiKhoanID=gv.TaiKhoanID WHERE tk.TenDangNhap LIKE @prefix;
        DELETE gv FROM GiaoVien gv JOIN TaiKhoan tk ON tk.TaiKhoanID=gv.TaiKhoanID WHERE tk.TenDangNhap LIKE @prefix;
        DELETE hv FROM HocVien hv JOIN TaiKhoan tk ON tk.TaiKhoanID=hv.TaiKhoanID WHERE tk.TenDangNhap LIKE @prefix;
        DELETE FROM TaiKhoan WHERE TenDangNhap LIKE @prefix;",
        p => p.AddWithValue("@prefix", Prefix + "%"));
}

PersonRecord Valid(string role, DateTime? birth = null)
{
    var id = ++sequence;
    var item = new PersonRecord
    {
        VaiTro = role,
        TenDangNhap = $"{Prefix}{(role == "HocVien" ? "hv" : "gv")}_{id:000}",
        Email = $"{Prefix}{id:000}@example.test",
        HoTen = role == "HocVien" ? "Học Viên Kiểm Thử" : "Giáo Viên Kiểm Thử",
        SoDienThoai = "0912345678",
        NgaySinh = birth ?? new DateTime(2000, 1, 1),
        LoaiHocVien = "HocSinh",
        LoaiGoi = "MienPhi",
        TrangThai = "HoatDong",
        MonHocIds = role == "GiaoVien" ? [subjectId] : []
    };
    foreach (var field in ProfileFields.ForRole(role))
        item.HoSo[field.Key] = field.Key switch
        {
            "DiaChi" => "123 Đường Kiểm Thử, Quận 1",
            "GioiTinh" => "Nam",
            "TinhTrangCha" or "TinhTrangMe" => "",
            "HoTenCha" or "HoTenMe" or "SdtCha" or "SdtMe" => "",
            "NguoiLienHe" => "Nguyễn Văn Liên Hệ",
            "QuanHeLienHe" => "Anh/Chị/Em",
            "SdtLienHe" => "0987654321",
            "GhiChu" => "",
            "HoTenNguoiGiamHo" or "QuanHeNguoiGiamHo" or "SdtNguoiGiamHo" => "",
            "TrinhDoHocVan" => "Đại học",
            "BangCap" => "Cử nhân",
            "TruongHoc" => "Trường Đại học Kiểm Thử",
            "ChuyenNganh" => "Sư phạm",
            "DonViCongTac" => "Trường THPT Kiểm Thử",
            _ => "Dữ liệu kiểm thử"
        };
    return item;
}

void Pass(string name)
{
    passed++;
    Console.WriteLine("PASS " + name);
}

void Fail(string name, string detail)
{
    failed.Add(name + ": " + detail);
    Console.WriteLine("FAIL " + name + ": " + detail);
}

(bool Ok, int Id, string Message) Save(PersonRecord item, string password = "TemporaryPass123!")
{
    var result = service.Luu(item, password);
    return (result.Ok, result.TaiKhoanID, result.Message);
}

void ExpectSuccess(string name, PersonRecord item, Action<PersonRecord>? verify = null)
{
    var result = Save(item);
    if (!result.Ok) { Fail(name, result.Message); return; }
    var saved = service.LayChiTiet(item.VaiTro, result.Id);
    if (saved == null) { Fail(name, "Lưu thành công nhưng không đọc lại được hồ sơ."); return; }
    try { verify?.Invoke(saved); Pass(name); }
    catch (Exception ex) { Fail(name, ex.Message); }
}

void ExpectFailure(string name, PersonRecord item, string? expected = null, string password = "TemporaryPass123!")
{
    var result = Save(item, password);
    if (result.Ok) { Fail(name, "Hệ thống đã chấp nhận dữ liệu không hợp lệ."); return; }
    if (expected != null && !result.Message.Contains(expected, StringComparison.OrdinalIgnoreCase))
    { Fail(name, $"Thông báo không đúng. Nhận: {result.Message}"); return; }
    Pass(name);
}

try
{
    Cleanup();

    // Hai luồng tạo hợp lệ cơ bản.
    ExpectSuccess("Học viên đủ 18 tuổi không cần thông tin cha mẹ", Valid("HocVien"));
    ExpectSuccess("Giáo viên có hồ sơ chuyên môn và môn phụ trách", Valid("GiaoVien"), saved =>
    {
        if (saved.HoSo.ContainsKey("TinhTrangCha")) throw new Exception("Hồ sơ giáo viên vẫn chứa trường cha mẹ.");
    });

    // Trường tài khoản và hồ sơ cơ bản áp dụng cho cả hai vai trò.
    foreach (var role in new[] { "HocVien", "GiaoVien" })
    {
        var label = role == "HocVien" ? "HV" : "GV";
        var item = Valid(role); item.TenDangNhap = ""; ExpectFailure(label + " thiếu tên đăng nhập", item, "Tên đăng nhập");
        item = Valid(role); item.TenDangNhap = "ab"; ExpectFailure(label + " tên đăng nhập quá ngắn", item, "3–50");
        item = Valid(role); item.TenDangNhap = "ten co khoang trang"; ExpectFailure(label + " tên đăng nhập có khoảng trắng", item, "khoảng trắng");
        item = Valid(role); item.Email = "email-sai"; ExpectFailure(label + " email sai định dạng", item, "Email");
        item = Valid(role); item.HoTen = ""; ExpectFailure(label + " thiếu họ tên", item, "Họ tên");
        item = Valid(role); item.SoDienThoai = null; ExpectFailure(label + " thiếu số điện thoại", item, "Số điện thoại");
        item = Valid(role); item.SoDienThoai = "12abc"; ExpectFailure(label + " số điện thoại sai", item, "Số điện thoại");
        item = Valid(role); item.NgaySinh = null; ExpectFailure(label + " thiếu ngày sinh", item, "Ngày sinh");
        item = Valid(role); item.NgaySinh = DateTime.Today.AddDays(1); ExpectFailure(label + " ngày sinh tương lai", item, "Ngày sinh");
        item = Valid(role); item.NgaySinh = new DateTime(1899, 12, 31); ExpectFailure(label + " ngày sinh trước năm 1900", item, "Ngày sinh");
        item = Valid(role); item.TrangThai = "Sai"; ExpectFailure(label + " trạng thái tài khoản sai", item, "Trạng thái");
        item = Valid(role); ExpectFailure(label + " mật khẩu dưới 6 ký tự", item, "6 ký tự", "12345");

        foreach (var field in ProfileFields.RequiredForRole(role))
        {
            item = Valid(role);
            item.HoSo[field.Key] = "";
            ExpectFailure($"{label} thiếu trường bắt buộc: {field.Caption}", item, "bắt buộc");
        }
    }

    // Quy tắc riêng của học viên.
    var student = Valid("HocVien"); student.LoaiHocVien = "Sai";
    ExpectFailure("Học viên có loại hồ sơ sai", student, "loại học viên");
    student = Valid("HocVien"); student.LoaiGoi = "Sai";
    ExpectFailure("Học viên có gói sử dụng sai", student, "Gói học viên");
    student = Valid("HocVien"); student.HoSo["TinhTrangCha"] = "Sai";
    ExpectFailure("Tình trạng cha không hợp lệ", student, "tình trạng của cha");
    student = Valid("HocVien"); student.HoSo["TinhTrangCha"] = "Còn sống"; student.HoSo["SdtCha"] = "0912345678";
    ExpectFailure("Cha còn sống nhưng thiếu họ tên", student, "Họ tên cha");
    student = Valid("HocVien"); student.HoSo["TinhTrangCha"] = "Còn sống"; student.HoSo["HoTenCha"] = "Nguyễn Văn Cha";
    ExpectFailure("Cha còn sống nhưng thiếu số điện thoại", student, "Số điện thoại cha");
    student = Valid("HocVien"); student.HoSo["TinhTrangMe"] = "Còn sống"; student.HoSo["HoTenMe"] = "Nguyễn Thị Mẹ"; student.HoSo["SdtMe"] = "abc";
    ExpectFailure("Số điện thoại mẹ không hợp lệ", student, "Số điện thoại mẹ");
    student = Valid("HocVien"); student.HoSo["TinhTrangCha"] = "Đã mất";
    ExpectSuccess("Cha đã mất không yêu cầu số điện thoại", student);
    student = Valid("HocVien"); student.HoSo["TinhTrangCha"] = "Không rõ thông tin"; student.HoSo["HoTenCha"] = "Dữ liệu phải xóa"; student.HoSo["SdtCha"] = "0912345678";
    ExpectSuccess("Không rõ thông tin sẽ xóa tên và SĐT cha", student, saved =>
    {
        if (saved.HoSo["HoTenCha"] != "" || saved.HoSo["SdtCha"] != "") throw new Exception("Dữ liệu cha chưa được xóa.");
    });

    var minorBirth = DateTime.Today.AddYears(-16);
    student = Valid("HocVien", minorBirth);
    ExpectFailure("Dưới 18 tuổi không có cha mẹ hoặc giám hộ", student, "dưới 18 tuổi");
    student = Valid("HocVien", minorBirth); student.HoSo["TinhTrangCha"] = "Còn sống"; student.HoSo["HoTenCha"] = "Nguyễn Văn Cha"; student.HoSo["SdtCha"] = "0912345678";
    ExpectSuccess("Dưới 18 tuổi có thông tin cha", student);
    student = Valid("HocVien", minorBirth); student.HoSo["TinhTrangMe"] = "Còn sống"; student.HoSo["HoTenMe"] = "Nguyễn Thị Mẹ"; student.HoSo["SdtMe"] = "0912345678";
    ExpectSuccess("Dưới 18 tuổi có thông tin mẹ", student);
    student = Valid("HocVien", minorBirth); student.HoSo["HoTenNguoiGiamHo"] = "Nguyễn Văn Anh"; student.HoSo["QuanHeNguoiGiamHo"] = "Anh/Chị/Em"; student.HoSo["SdtNguoiGiamHo"] = "0912345678";
    ExpectFailure("Người giám hộ không thay thế cha mẹ khi chưa khai cả hai đã mất", student, "ít nhất một cha hoặc mẹ");
    student = Valid("HocVien", minorBirth); student.HoSo["TinhTrangCha"] = "Đã mất"; student.HoSo["TinhTrangMe"] = "Đã mất";
    ExpectFailure("Cả cha mẹ đã mất nhưng chưa chọn người giám hộ", student, "người giám hộ");
    student = Valid("HocVien", minorBirth); student.HoSo["TinhTrangCha"] = "Đã mất"; student.HoSo["TinhTrangMe"] = "Đã mất"; student.HoSo["HoTenNguoiGiamHo"] = "Nguyễn Văn Anh"; student.HoSo["QuanHeNguoiGiamHo"] = "Anh/Chị/Em"; student.HoSo["SdtNguoiGiamHo"] = "0912345678";
    ExpectSuccess("Cả cha mẹ đã mất, anh/chị là người giám hộ", student, saved =>
    {
        if (saved.HoSo["QuanHeNguoiGiamHo"] != "Anh/Chị/Em" || saved.HoSo["HoTenNguoiGiamHo"] != "Nguyễn Văn Anh")
            throw new Exception("Không giữ đúng quan hệ và vai trò người giám hộ.");
    });
    student = Valid("HocVien", DateTime.Today.AddYears(-18));
    ExpectSuccess("Đúng ngày đủ 18 tuổi không cần cha mẹ hoặc giám hộ", student);
    student = Valid("HocVien", DateTime.Today.AddYears(-20)); student.HoSo["TinhTrangCha"] = "Đã mất"; student.HoSo["TinhTrangMe"] = "Đã mất"; student.HoSo["HoTenNguoiGiamHo"] = "Dữ liệu ẩn"; student.HoSo["QuanHeNguoiGiamHo"] = "Anh/Chị/Em"; student.HoSo["SdtNguoiGiamHo"] = "0912345678";
    ExpectSuccess("Đủ 18 tuổi và cả cha mẹ đã mất không yêu cầu người giám hộ", student, saved =>
    {
        if (saved.HoSo["HoTenNguoiGiamHo"] != "" || saved.HoSo["SdtNguoiGiamHo"] != "")
            throw new Exception("Thông tin người giám hộ ẩn vẫn được lưu.");
    });

    // Quy tắc riêng của giáo viên.
    var teacher = Valid("GiaoVien"); teacher.MonHocIds = [];
    ExpectFailure("Giáo viên không chọn môn phụ trách", teacher, "ít nhất một môn");
    teacher = Valid("GiaoVien"); teacher.MonHocIds = [int.MaxValue];
    ExpectFailure("Giáo viên chọn môn không tồn tại", teacher, "không còn tồn tại");

    // Ràng buộc duy nhất của tài khoản.
    var original = Valid("HocVien");
    var originalResult = Save(original);
    if (!originalResult.Ok) Fail("Tạo dữ liệu kiểm tra trùng", originalResult.Message);
    else
    {
        var duplicate = Valid("HocVien"); duplicate.TenDangNhap = original.TenDangNhap;
        ExpectFailure("Không cho phép trùng tên đăng nhập", duplicate, "đã tồn tại");
        duplicate = Valid("HocVien"); duplicate.Email = original.Email;
        ExpectFailure("Không cho phép trùng email", duplicate, "đã tồn tại");
    }
}
finally
{
    Cleanup();
}

Console.WriteLine($"\nKẾT QUẢ: {passed} đạt, {failed.Count} lỗi trên database riêng {database}.");
if (failed.Count > 0)
{
    Console.WriteLine(string.Join(Environment.NewLine, failed.Select(x => " - " + x)));
    Environment.ExitCode = 1;
}
