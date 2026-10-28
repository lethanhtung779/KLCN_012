using System.Data;
using Microsoft.Data.SqlClient;

namespace QLThiTN.WinForms.Services;

public sealed class PersonRecord
{
    public Dictionary<string, string> HoSo { get; set; } = [];
    public int TaiKhoanID { get; set; }
    public string VaiTro { get; set; } = "HocVien";
    public string TenDangNhap { get; set; } = "";
    public string Email { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string? SoDienThoai { get; set; }
    public DateTime? NgaySinh { get; set; }
    public string LoaiHocVien { get; set; } = "HocSinh";
    public string LoaiGoi { get; set; } = "MienPhi";
    public string TrangThai { get; set; } = "HoatDong";
    public List<int> MonHocIds { get; set; } = [];
}

/// <summary>Quản lý hồ sơ học viên/giáo viên cùng tài khoản đăng nhập trong một giao dịch.</summary>
public sealed class PersonManagementService
{
    public DataTable LayDanhSach(string vaiTro, string? tuKhoa, string? trangThai)
    {
        Session.YeuCauQuanTriVien();
        if (vaiTro == "HocVien")
            return Database.Query(@"
                SELECT tk.TaiKhoanID, CONCAT('TK', FORMAT(tk.TaiKhoanID, '000000')) AS MaTaiKhoan,
                       hv.HocVienID, hv.HoTen, tk.TenDangNhap, tk.Email,
                       hv.SoDienThoai, hv.NgaySinh, hv.LoaiHocVien, hv.LoaiGoi, tk.TrangThai,
                       CASE hv.LoaiHocVien WHEN N'HocSinh' THEN N'Học sinh' WHEN N'SinhVien' THEN N'Sinh viên'
                           ELSE N'Tự do' END AS LoaiHocVienHienThi,
                       CASE hv.LoaiGoi WHEN N'DongPhi' THEN N'Đóng phí (VIP)' ELSE N'Miễn phí' END AS LoaiGoiHienThi,
                       CASE tk.TrangThai WHEN N'Khoa' THEN N'Đã khóa' ELSE N'Hoạt động' END AS TrangThaiHienThi,
                       (SELECT COUNT(*) FROM HocVien_Lop hl WHERE hl.HocVienID=hv.HocVienID
                           AND hl.TrangThai=N'DangHoc') AS SoLop, tk.NgayTao
                FROM HocVien hv JOIN TaiKhoan tk ON tk.TaiKhoanID=hv.TaiKhoanID
                WHERE tk.VaiTro=N'HocVien'
                  AND (@id = 0 OR tk.TaiKhoanID = @id)
                  AND (@status=N'' OR tk.TrangThai=@status)
                  AND (@q=N'' OR hv.HoTen LIKE N'%'+@q+N'%' OR tk.TenDangNhap LIKE N'%'+@q+N'%'
                      OR tk.Email LIKE N'%'+@q+N'%' OR hv.SoDienThoai LIKE N'%'+@q+N'%')
                ORDER BY hv.HocVienID DESC", p => BindFilter(p, tuKhoa, trangThai));
        if (vaiTro == "GiaoVien")
            return Database.Query(@"
                SELECT tk.TaiKhoanID, CONCAT('TK', FORMAT(tk.TaiKhoanID, '000000')) AS MaTaiKhoan,
                       gv.GiaoVienID, gv.HoTen, tk.TenDangNhap, tk.Email,
                       gv.SoDienThoai, tk.TrangThai,
                       CASE tk.TrangThai WHEN N'Khoa' THEN N'Đã khóa' ELSE N'Hoạt động' END AS TrangThaiHienThi,
                       STUFF((SELECT N', ' + mh.TenMon FROM GiaoVien_MonHoc gm
                           JOIN MonHoc mh ON mh.MonHocID=gm.MonHocID
                           WHERE gm.GiaoVienID=gv.GiaoVienID ORDER BY mh.TenMon
                           FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, N'') AS MonPhuTrach,
                       (SELECT COUNT(*) FROM PhanCongGiangDay pc WHERE pc.GiaoVienID=gv.GiaoVienID) AS SoLop,
                       (SELECT COUNT(*) FROM CauHoi c WHERE c.NguoiTaoID=gv.TaiKhoanID) AS SoCauHoi,
                       tk.NgayTao
                FROM GiaoVien gv JOIN TaiKhoan tk ON tk.TaiKhoanID=gv.TaiKhoanID
                WHERE tk.VaiTro=N'GiaoVien'
                  AND (@id = 0 OR tk.TaiKhoanID = @id)
                  AND (@status=N'' OR tk.TrangThai=@status)
                  AND (@q=N'' OR gv.HoTen LIKE N'%'+@q+N'%' OR tk.TenDangNhap LIKE N'%'+@q+N'%'
                      OR tk.Email LIKE N'%'+@q+N'%' OR gv.SoDienThoai LIKE N'%'+@q+N'%'
                      OR EXISTS (SELECT 1 FROM GiaoVien_MonHoc gm JOIN MonHoc mh ON mh.MonHocID=gm.MonHocID
                          WHERE gm.GiaoVienID=gv.GiaoVienID AND mh.TenMon LIKE N'%'+@q+N'%'))
                ORDER BY gv.GiaoVienID DESC", p => BindFilter(p, tuKhoa, trangThai));
        throw new ArgumentException("Vai trò không hợp lệ.", nameof(vaiTro));
    }

    private static void BindFilter(SqlParameterCollection p, string? tuKhoa, string? trangThai)
    {
        var id = DirectoryRules.SearchId(tuKhoa, "TK");
        p.AddWithValue("@id", id);
        p.AddWithValue("@q", id > 0 ? "" : tuKhoa?.Trim() ?? "");
        p.AddWithValue("@status", trangThai ?? "");
    }

    public DataTable LayDanhSachMonHoc()
    {
        Session.YeuCauQuanTriVien();
        return Database.Query("SELECT MonHocID, TenMon FROM MonHoc ORDER BY TenMon");
    }

    public PersonRecord? LayChiTiet(string vaiTro, int taiKhoanId)
    {
        Session.YeuCauQuanTriVien();
        if (vaiTro is not ("HocVien" or "GiaoVien")) throw new ArgumentException("Vai trò không hợp lệ.");
        var profile = vaiTro == "HocVien" ? "HocVien" : "GiaoVien";
        var extras = vaiTro == "HocVien"
            ? "p.NgaySinh, p.LoaiHocVien, p.LoaiGoi"
            : "p.NgaySinh, CAST(NULL AS nvarchar(50)) AS LoaiHocVien, CAST(NULL AS nvarchar(20)) AS LoaiGoi";
        var profileFields = ProfileFields.ForRole(vaiTro);
        var data = Database.Query($@"
            SELECT tk.TaiKhoanID, tk.VaiTro, tk.TenDangNhap, tk.Email, tk.TrangThai,
                   p.HoTen, p.SoDienThoai, {extras}, {string.Join(",", profileFields.Select(f => "p." + f.Key))}
            FROM TaiKhoan tk JOIN {profile} p ON p.TaiKhoanID=tk.TaiKhoanID
            WHERE tk.TaiKhoanID=@id AND tk.VaiTro=@role", p =>
        {
            p.AddWithValue("@id", taiKhoanId);
            p.AddWithValue("@role", vaiTro);
        });
        if (data.Rows.Count == 0) return null;
        var r = data.Rows[0];
        var item = new PersonRecord
        {
            TaiKhoanID = taiKhoanId,
            VaiTro = vaiTro,
            TenDangNhap = r["TenDangNhap"].ToString() ?? "",
            Email = r["Email"].ToString() ?? "",
            HoTen = r["HoTen"].ToString() ?? "",
            SoDienThoai = r["SoDienThoai"] == DBNull.Value ? null : r["SoDienThoai"].ToString(),
            NgaySinh = r["NgaySinh"] == DBNull.Value ? null : Convert.ToDateTime(r["NgaySinh"]),
            LoaiHocVien = r["LoaiHocVien"] == DBNull.Value ? "HocSinh" : r["LoaiHocVien"].ToString()!,
            LoaiGoi = r["LoaiGoi"] == DBNull.Value ? "MienPhi" : r["LoaiGoi"].ToString()!,
            TrangThai = r["TrangThai"].ToString() ?? "HoatDong"
        };
        foreach (var field in profileFields) item.HoSo[field.Key] = r[field.Key].ToString() ?? "";
        if (vaiTro == "GiaoVien")
            item.MonHocIds = Database.ReadList(@"
                SELECT gm.MonHocID FROM GiaoVien_MonHoc gm
                JOIN GiaoVien gv ON gv.GiaoVienID=gm.GiaoVienID
                WHERE gv.TaiKhoanID=@id", reader => reader.GetInt32(0),
                p => p.AddWithValue("@id", taiKhoanId));
        return item;
    }

    public (bool Ok, string Message, int TaiKhoanID) Luu(PersonRecord item, string? matKhau)
    {
        Session.YeuCauQuanTriVien();
        if (item.VaiTro is not ("HocVien" or "GiaoVien")) return (false, "Vai trò không hợp lệ.", 0);
        item.TenDangNhap = item.TenDangNhap.Trim();
        item.Email = item.Email.Trim();
        item.HoTen = item.HoTen.Trim();
        item.SoDienThoai = string.IsNullOrWhiteSpace(item.SoDienThoai) ? null : item.SoDienThoai.Trim();
        var profileFields = ProfileFields.ForRole(item.VaiTro);
        foreach (var field in profileFields)
        {
            var value = item.HoSo.GetValueOrDefault(field.Key, "").Trim();
            if (value.Length > 500) return (false, field.Caption + " tối đa 500 ký tự.", 0);
            if (field.Key.StartsWith("Sdt") && value.Length > 0 &&
                (value.Count(char.IsDigit) is < 9 or > 15 || value.Any(c => !char.IsDigit(c) && c is not ('+' or '-' or ' ' or '(' or ')'))))
                return (false, field.Caption + " không hợp lệ (9–15 chữ số).", 0);
            item.HoSo[field.Key] = value;
        }
        if (item.NgaySinh is { } date && (date.Date > DateTime.Today || date.Year < 1900))
            return (false, "Ngày sinh không hợp lệ.", 0);
        if (item.TaiKhoanID == 0 && (item.TenDangNhap.Length is < 3 or > 50 || item.TenDangNhap.Any(char.IsWhiteSpace)))
            return (false, "Tên đăng nhập phải có 3–50 ký tự và không chứa khoảng trắng.", 0);
        if (item.HoTen.Length is < 2 or > 100) return (false, "Họ tên phải có 2–100 ký tự.", 0);
        if (!DirectoryRules.ValidEmail(item.Email))
            return (false, "Email chưa đầy đủ. Ví dụ: hoten@gmail.com (tối đa 100 ký tự).", 0);
        if (item.SoDienThoai != null && (item.SoDienThoai.Length is < 9 or > 20 ||
            item.SoDienThoai.Any(c => !char.IsDigit(c) && c is not ('+' or '-' or ' '))))
            return (false, "Số điện thoại phải có 9–20 ký tự gồm số, dấu +, dấu - hoặc khoảng trắng.", 0);
        if (item.TaiKhoanID == 0)
        {
            if (item.SoDienThoai == null) return (false, "Số điện thoại là thông tin bắt buộc.", 0);
            if (!item.NgaySinh.HasValue) return (false, "Ngày sinh là thông tin bắt buộc.", 0);
            var missing = ProfileFields.RequiredForRole(item.VaiTro)
                .FirstOrDefault(field => string.IsNullOrWhiteSpace(item.HoSo.GetValueOrDefault(field.Key)));
            if (!string.IsNullOrEmpty(missing.Key))
                return (false, missing.Caption + " là thông tin bắt buộc.", 0);
        }
        if (item.TrangThai is not ("HoatDong" or "Khoa")) return (false, "Trạng thái không hợp lệ.", 0);
        if (item.TaiKhoanID == 0 && (matKhau?.Length ?? 0) < 6)
            return (false, "Mật khẩu mới phải có ít nhất 6 ký tự.", 0);
        if (!string.IsNullOrEmpty(matKhau) && matKhau.Length < 6)
            return (false, "Mật khẩu phải có ít nhất 6 ký tự.", 0);
        if (item.VaiTro == "HocVien")
        {
            if (item.LoaiHocVien is not ("HocSinh" or "TuDo")) return (false, "Chọn loại học viên: Học sinh hoặc Tự do.", 0);
            if (item.LoaiGoi is not ("MienPhi" or "DongPhi")) return (false, "Gói học viên không hợp lệ.", 0);
            if (item.NgaySinh is { } birth && (birth.Date > DateTime.Today || birth.Year < 1900))
                return (false, "Ngày sinh phải nằm trong khoảng từ năm 1900 đến hôm nay.", 0);
        }
        else
        {
            item.MonHocIds = item.MonHocIds.Distinct().ToList();
            if (item.MonHocIds.Count == 0) return (false, "Hãy chọn ít nhất một môn để giáo viên thấy ngân hàng câu hỏi.", 0);
            var allSubjects = LayDanhSachMonHoc().Rows.Cast<DataRow>().Select(r => Convert.ToInt32(r["MonHocID"])).ToHashSet();
            if (item.MonHocIds.Any(id => !allSubjects.Contains(id))) return (false, "Có môn học không còn tồn tại.", 0);
        }

        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();
        var oldPackage = "MienPhi";
        try
        {
            int id = item.TaiKhoanID;
            if (id == 0)
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO TaiKhoan(TenDangNhap,MatKhau,Email,VaiTro,TrangThai,NgayTao)
                    VALUES(@user,@password,@email,@role,@status,SYSDATETIME());
                    SELECT CAST(SCOPE_IDENTITY() AS int);", conn, tx);
                cmd.Parameters.AddWithValue("@user", item.TenDangNhap);
                cmd.Parameters.AddWithValue("@password", PasswordHelper.Hash(matKhau!));
                cmd.Parameters.AddWithValue("@email", item.Email);
                cmd.Parameters.AddWithValue("@role", item.VaiTro);
                cmd.Parameters.AddWithValue("@status", item.TrangThai);
                id = Convert.ToInt32(cmd.ExecuteScalar());
                if (item.VaiTro == "HocVien")
                {
                    using var profile = new SqlCommand(@"
                        INSERT INTO HocVien(TaiKhoanID,HoTen,SoDienThoai,NgaySinh,LoaiHocVien,LoaiGoi)
                        VALUES(@id,@name,@phone,@birth,@type,@package)", conn, tx);
                    BindStudent(profile, id, item);
                    profile.ExecuteNonQuery();
                }
                else
                {
                    using var profile = new SqlCommand(@"
                        INSERT INTO GiaoVien(TaiKhoanID,HoTen,SoDienThoai) VALUES(@id,@name,@phone)", conn, tx);
                    BindTeacher(profile, id, item);
                    profile.ExecuteNonQuery();
                }
            }
            else
            {
                using var find = new SqlCommand(@"
                    SELECT tk.VaiTro, tk.TenDangNhap, hv.LoaiGoi FROM TaiKhoan tk
                    LEFT JOIN HocVien hv ON hv.TaiKhoanID=tk.TaiKhoanID WHERE tk.TaiKhoanID=@id", conn, tx);
                find.Parameters.AddWithValue("@id", id);
                using var reader = find.ExecuteReader();
                if (!reader.Read() || reader.GetString(0) != item.VaiTro || reader.GetString(1) != item.TenDangNhap)
                {
                    reader.Close();
                    tx.Rollback();
                    return (false, "Hồ sơ đã thay đổi hoặc không còn tồn tại. Hãy tải lại danh sách.", 0);
                }
                oldPackage = reader.IsDBNull(2) ? "MienPhi" : reader.GetString(2);
                reader.Close();

                using var account = new SqlCommand(@"
                    UPDATE TaiKhoan SET Email=@email, TrangThai=@status
                    WHERE TaiKhoanID=@id", conn, tx);
                account.Parameters.AddWithValue("@email", item.Email);
                account.Parameters.AddWithValue("@status", item.TrangThai);
                account.Parameters.AddWithValue("@id", id);
                account.ExecuteNonQuery();
                if (!string.IsNullOrEmpty(matKhau))
                {
                    using var password = new SqlCommand("UPDATE TaiKhoan SET MatKhau=@password WHERE TaiKhoanID=@id", conn, tx);
                    password.Parameters.AddWithValue("@password", PasswordHelper.Hash(matKhau));
                    password.Parameters.AddWithValue("@id", id);
                    password.ExecuteNonQuery();
                }
                if (item.VaiTro == "HocVien")
                {
                    using var profile = new SqlCommand(@"
                        UPDATE HocVien SET HoTen=@name,SoDienThoai=@phone,NgaySinh=@birth,
                            LoaiHocVien=@type,LoaiGoi=@package WHERE TaiKhoanID=@id", conn, tx);
                    BindStudent(profile, id, item);
                    if (profile.ExecuteNonQuery() != 1) throw new InvalidOperationException("Không tìm thấy hồ sơ học viên.");
                }
                else
                {
                    using var profile = new SqlCommand(@"
                        UPDATE GiaoVien SET HoTen=@name,SoDienThoai=@phone WHERE TaiKhoanID=@id", conn, tx);
                    BindTeacher(profile, id, item);
                    if (profile.ExecuteNonQuery() != 1) throw new InvalidOperationException("Không tìm thấy hồ sơ giáo viên.");
                }
            }
            if (item.VaiTro == "GiaoVien")
            {
                using var required = new SqlCommand(@"
                    SELECT DISTINCT MonHocID FROM PhanCongGiangDay WHERE GiaoVienID=(SELECT GiaoVienID FROM GiaoVien WHERE TaiKhoanID=@id)
                    UNION SELECT DISTINCT cd.MonHocID FROM CauHoi c JOIN ChuDe cd ON cd.ChuDeID=c.ChuDeID
                    WHERE c.NguoiTaoID=@id", conn, tx);
                required.Parameters.AddWithValue("@id", id);
                using var reader = required.ExecuteReader();
                var requiredIds = new List<int>();
                while (reader.Read()) requiredIds.Add(reader.GetInt32(0));
                reader.Close();
                if (requiredIds.Any(subject => !item.MonHocIds.Contains(subject)))
                {
                    tx.Rollback();
                    return (false, "Không thể bỏ môn đang có câu hỏi hoặc lớp được phân công.", 0);
                }
                using var clearSubjects = new SqlCommand(@"
                    DELETE FROM GiaoVien_MonHoc WHERE GiaoVienID=(SELECT GiaoVienID FROM GiaoVien WHERE TaiKhoanID=@id)", conn, tx);
                clearSubjects.Parameters.AddWithValue("@id", id);
                clearSubjects.ExecuteNonQuery();
                using var saveSubjects = new SqlCommand(@"
                    INSERT INTO GiaoVien_MonHoc(GiaoVienID,MonHocID)
                    SELECT gv.GiaoVienID,@subject FROM GiaoVien gv WHERE gv.TaiKhoanID=@id", conn, tx);
                saveSubjects.Parameters.AddWithValue("@id", id);
                saveSubjects.Parameters.Add("@subject", SqlDbType.Int);
                foreach (var subjectId in item.MonHocIds)
                {
                    saveSubjects.Parameters["@subject"].Value = subjectId;
                    saveSubjects.ExecuteNonQuery();
                }
            }
            using (var extra = new SqlCommand($"UPDATE {item.VaiTro} SET NgaySinh=@birth," +
                string.Join(",", profileFields.Select(f => $"[{f.Key}]=@{f.Key}")) + " WHERE TaiKhoanID=@id", conn, tx))
            {
                extra.Parameters.AddWithValue("@id", id);
                extra.Parameters.AddWithValue("@birth", (object?)item.NgaySinh?.Date ?? DBNull.Value);
                foreach (var field in profileFields)
                    extra.Parameters.AddWithValue("@" + field.Key, string.IsNullOrWhiteSpace(item.HoSo[field.Key]) ? DBNull.Value : item.HoSo[field.Key]);
                extra.ExecuteNonQuery();
            }
            tx.Commit();
            if (item.VaiTro == "HocVien" && oldPackage != "DongPhi" && item.LoaiGoi == "DongPhi")
            {
                try
                {
                    var (sent, emailError) = VipEmailSender.GuiNangVip(item.Email, item.HoTen);
                    return (true, sent ? "Đã lưu hồ sơ và gửi email nâng VIP." : "Đã lưu hồ sơ; chưa gửi được email nâng VIP: " + emailError, id);
                }
                catch (Exception ex)
                {
                    return (true, "Đã lưu hồ sơ; chưa gửi được email nâng VIP: " + ex.Message, id);
                }
            }
            return (true, "Đã lưu hồ sơ.", id);
        }
        catch (SqlException ex)
        {
            try { tx.Rollback(); } catch { }
            return (false, ex.Number is 2601 or 2627
                ? "Tên đăng nhập hoặc email đã tồn tại."
                : "Không lưu được hồ sơ: " + ex.Message, 0);
        }
        catch (Exception ex)
        {
            try { tx.Rollback(); } catch { }
            return (false, "Không lưu được hồ sơ: " + ex.Message, 0);
        }
    }

    private static void BindStudent(SqlCommand cmd, int id, PersonRecord item)
    {
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", item.HoTen);
        cmd.Parameters.AddWithValue("@phone", (object?)item.SoDienThoai ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@birth", (object?)item.NgaySinh?.Date ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@type", item.LoaiHocVien);
        cmd.Parameters.AddWithValue("@package", item.LoaiGoi);
    }

    private static void BindTeacher(SqlCommand cmd, int id, PersonRecord item)
    {
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@name", item.HoTen);
        cmd.Parameters.AddWithValue("@phone", (object?)item.SoDienThoai ?? DBNull.Value);
    }
}
