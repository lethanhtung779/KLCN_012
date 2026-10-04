using System.Data;
using QLThiTN.WinForms.Models;

namespace QLThiTN.WinForms.Services;

/// <summary>Quan ly ngan hang cau hoi: loc, CRUD ca 3 loai cau hoi, import tu file.</summary>
public class QuestionService
{
    // ------------------------------------------------------------------ danh muc
    public List<MonHoc> LayMonHoc() =>
        Database.ReadList("SELECT MonHocID, TenMon FROM MonHoc ORDER BY MonHocID",
            r => new MonHoc { MonHocID = r.GetInt32(0), TenMon = r.GetString(1) });

    public List<ChuDe> LayChuDe(int? monHocId = null)
    {
        var sql = monHocId.HasValue
            ? "SELECT ChuDeID, MonHocID, TenChuDe FROM ChuDe WHERE MonHocID = @m ORDER BY TenChuDe"
            : "SELECT ChuDeID, MonHocID, TenChuDe FROM ChuDe ORDER BY TenChuDe";
        return Database.ReadList(sql,
            r => new ChuDe { ChuDeID = r.GetInt32(0), MonHocID = r.GetInt32(1), TenChuDe = r.GetString(2) },
            p => { if (monHocId.HasValue) p.AddWithValue("@m", monHocId.Value); });
    }

    // ------------------------------------------------------------------ tra cuu
    public DataTable TraCuu(int? monHocId, int? chuDeId, string? loai, string? mucDo, string? tuKhoa)
    {
        var sql = @"
            SELECT c.CauHoiID, c.NoiDung, c.LoaiCauHoi, c.MucDoKho, c.Diem, c.TrangThai,
                   cd.TenChuDe, m.TenMon, gv.HoTen AS TenGiaoVien, c.NgayTao, c.ChuDeID, c.GiaoVienID
            FROM CauHoi c
            JOIN ChuDe cd ON cd.ChuDeID = c.ChuDeID
            JOIN MonHoc m ON m.MonHocID = cd.MonHocID
            LEFT JOIN GiaoVien gv ON gv.GiaoVienID = c.GiaoVienID
            WHERE (@mon = 0 OR cd.MonHocID = @mon)
              AND (@chuDe = 0 OR c.ChuDeID = @chuDe)
              AND (@loai = N'' OR c.LoaiCauHoi = @loai)
              AND (@muc = N'' OR c.MucDoKho = @muc)
              AND (@tu = N'' OR c.NoiDung LIKE N'%' + @tu + N'%')
            ORDER BY c.CauHoiID DESC";
        return Database.Query(sql, p =>
        {
            p.AddWithValue("@mon", monHocId ?? 0);
            p.AddWithValue("@chuDe", chuDeId ?? 0);
            p.AddWithValue("@loai", loai ?? "");
            p.AddWithValue("@muc", mucDo ?? "");
            p.AddWithValue("@tu", tuKhoa?.Trim() ?? "");
        });
    }

    public CauHoi? LayChiTiet(int cauHoiId)
    {
        var cau = Database.ReadList(@"
            SELECT c.CauHoiID, c.ChuDeID, c.GiaoVienID, c.NoiDung, c.LoaiCauHoi, c.MucDoKho,
                   c.Diem, c.TrangThai, c.NgayTao, c.GiaiThichDapAn
            FROM CauHoi c WHERE c.CauHoiID = @id",
            r => new CauHoi
            {
                CauHoiID = r.GetInt32(0),
                ChuDeID = r.GetInt32(1),
                GiaoVienID = r.GetInt32(2),
                NoiDung = r.GetString(3),
                LoaiCauHoi = r.GetString(4),
                MucDoKho = r.GetString(5),
                Diem = r.GetDecimal(6),
                TrangThai = r.GetString(7),
                NgayTao = r.GetDateTime(8),
                GiaiThichDapAn = r.IsDBNull(9) ? null : r.GetString(9)
            },
            p => p.AddWithValue("@id", cauHoiId)).FirstOrDefault();

        if (cau == null) return null;

        cau.DapAns = Database.ReadList(
            "SELECT DapAnID, CauHoiID, ThuTu, NoiDung, LaDapAnDung FROM DapAn WHERE CauHoiID = @id ORDER BY ThuTu",
            r => new DapAn
            {
                DapAnID = r.GetInt32(0),
                CauHoiID = r.GetInt32(1),
                ThuTu = r.GetInt32(2),
                NoiDung = r.GetString(3),
                LaDapAnDung = r.GetBoolean(4)
            },
            p => p.AddWithValue("@id", cauHoiId));
        return cau;
    }

    // ------------------------------------------------------------------ CRUD
    /// <summary>Them moi cau hoi + dap an. GiaoVienID lay tu nguoi dang nhap.</summary>
    public int Them(CauHoi c)
    {
        c.GiaoVienID = Session.GiaoVienID;
        var id = Database.ExecuteScalarInt(@"
            INSERT INTO CauHoi (ChuDeID, GiaoVienID, NoiDung, LoaiCauHoi, MucDoKho, Diem, TrangThai, NgayTao, GiaiThichDapAn)
            VALUES (@cd, @gv, @nd, @loai, @muc, @diem, N'HoatDong', SYSDATETIME(), @gt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);",
            p =>
            {
                p.AddWithValue("@cd", c.ChuDeID);
                p.AddWithValue("@gv", c.GiaoVienID);
                p.AddWithValue("@nd", c.NoiDung);
                p.AddWithValue("@loai", c.LoaiCauHoi);
                p.AddWithValue("@muc", c.MucDoKho);
                p.AddWithValue("@diem", c.Diem);
                p.AddWithValue("@gt", (object?)c.GiaiThichDapAn ?? DBNull.Value);
            });
        LuuDapAn(id, c);
        return id;
    }

    public void Sua(CauHoi c)
    {
        Database.Execute(@"
            UPDATE CauHoi
            SET ChuDeID = @cd, NoiDung = @nd, LoaiCauHoi = @loai, MucDoKho = @muc,
                Diem = @diem, GiaiThichDapAn = @gt
            WHERE CauHoiID = @id",
            p =>
            {
                p.AddWithValue("@cd", c.ChuDeID);
                p.AddWithValue("@nd", c.NoiDung);
                p.AddWithValue("@loai", c.LoaiCauHoi);
                p.AddWithValue("@muc", c.MucDoKho);
                p.AddWithValue("@diem", c.Diem);
                p.AddWithValue("@gt", (object?)c.GiaiThichDapAn ?? DBNull.Value);
                p.AddWithValue("@id", c.CauHoiID);
            });
        LuuDapAn(c.CauHoiID, c);
    }

    private void LuuDapAn(int cauHoiId, CauHoi c)
    {
        Database.Execute("DELETE FROM LuaChonBaiLam WHERE ChiTietID IN (SELECT ChiTietID FROM ChiTietBaiLam WHERE CauHoiID = @id);\n" +
            "DELETE FROM ChiTietBaiLam WHERE CauHoiID = @id;\n" +
            "DELETE FROM DapAn WHERE CauHoiID = @id;",
            p => p.AddWithValue("@id", cauHoiId));

        for (var i = 0; i < c.DapAns.Count; i++)
        {
            var da = c.DapAns[i];
            if (string.IsNullOrWhiteSpace(da.NoiDung)) continue;
            Database.Execute(@"
                INSERT INTO DapAn (CauHoiID, ThuTu, NoiDung, LaDapAnDung)
                VALUES (@id, @t, @nd, @dung)",
                p =>
                {
                    p.AddWithValue("@id", cauHoiId);
                    p.AddWithValue("@t", i);
                    p.AddWithValue("@nd", da.NoiDung);
                    p.AddWithValue("@dung", da.LaDapAnDung);
                });
        }
    }

    /// <summary>Xoa cau hoi (chan xoa cau da co bai thi).</summary>
    public (bool Ok, string Error) Xoa(int cauHoiId)
    {
        var soBaiThi = Database.ExecuteScalarInt(
            "SELECT COUNT(1) FROM ChiTietBaiLam WHERE CauHoiID = @id",
            p => p.AddWithValue("@id", cauHoiId));
        if (soBaiThi > 0)
            return (false, $"Câu hỏi đã được dùng trong {soBaiThi} bài thi. Chỉ có thể ẩn (đổi trạng thái thành 'An').");

        Database.Execute(@"
            DELETE FROM LuaChonBaiLam WHERE ChiTietID IN (SELECT ChiTietID FROM ChiTietBaiLam WHERE CauHoiID = @id);
            DELETE FROM ChiTietBaiLam WHERE CauHoiID = @id;
            DELETE FROM DapAn WHERE CauHoiID = @id;
            DELETE FROM DeThi_CauHoi WHERE CauHoiID = @id;
            DELETE FROM CauHoi WHERE CauHoiID = @id;",
            p => p.AddWithValue("@id", cauHoiId));
        return (true, "");
    }

    /// <summary>An / hien cau hoi (An = khong hien trong kho de web).</summary>
    public void DoiTrangThai(int cauHoiId, bool an) =>
        Database.Execute("UPDATE CauHoi SET TrangThai = @t WHERE CauHoiID = @id",
            p => { p.AddWithValue("@t", an ? "An" : "HoatDong"); p.AddWithValue("@id", cauHoiId); });

    // ------------------------------------------------------------------ import file
    /// <summary>Tao file mau (.xlsx) voi header dinh dang de giao vien dien theo.</summary>
    public static readonly string[] HeaderImport =
    [
        "ChuDe (ten chinh xac)", "NoiDung", "LoaiCauHoi (TracNghiem|DungSai|TraLoiNgan)",
        "MucDoKho (NhanBiet|ThongHieu|VanDung|VanDungCao|ChuaXacDinh)", "Diem",
        "DapAnA", "DapAnB", "DapAnC", "DapAnD", "DapAnDung (A|B|C|D)",
        "Y_a_Dung (D|S)", "Y_b_Dung", "Y_c_Dung", "Y_d_Dung", "DapSo (TraLoiNgan)", "GiaiThich"
    ];

    public (int Them, int Loi, List<string> LoiChiTiet) Import(IEnumerable<(string ChuDe, string NoiDung, string Loai, string MucDo, string Diem,
        string Da, string Db, string Dc, string Dd, string Dung, string Ya, string Yb, string Yc, string Yd, string DapSo, string GiaiThich)> rows)
    {
        var monHocs = LayMonHoc();
        var chuDes = LayChuDe();
        var demThem = 0;
        var loi = new List<string>();

        foreach (var r in rows.Select((row, i) => (row, i: i + 2)))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(r.row.NoiDung)) { loi.Add($"Dòng {r.i}: thiếu nội dung."); continue; }

                var cd = chuDes.FirstOrDefault(x => string.Equals(x.TenChuDe.Trim(), r.row.ChuDe.Trim(), StringComparison.OrdinalIgnoreCase));
                if (cd == null) { loi.Add($"Dòng {r.i}: không tìm thấy chủ đề '{r.row.ChuDe.Trim()}'."); continue; }

                var loai = r.row.Loai.Trim() switch
                {
                    "TracNghiem" or "Trắc nghiệm" or "TN" => LoaiCauHoi.TracNghiem,
                    "DungSai" or "Đúng/Sai" or "Đúng sai" or "DS" => LoaiCauHoi.DungSai,
                    "TraLoiNgan" or "Trả lời ngắn" or "TLN" => LoaiCauHoi.TraLoiNgan,
                    _ => ""
                };
                if (loai == "") { loi.Add($"Dòng {r.i}: loại câu hỏi '{r.row.Loai}' không hợp lệ."); continue; }

                var muc = r.row.MucDo.Trim() switch
                {
                    "NhanBiet" or "Nhận biết" => MucDoKho.NhanBiet,
                    "ThongHieu" or "Thông hiểu" => MucDoKho.ThongHieu,
                    "VanDung" or "Vận dụng" => MucDoKho.VanDung,
                    "VanDungCao" or "Vận dụng cao" => MucDoKho.VanDungCao,
                    _ => MucDoKho.ChuaXacDinh
                };

                var diem = decimal.TryParse(r.row.Diem, out var d) && d > 0 ? d : 0.25m;

                var cau = new CauHoi
                {
                    ChuDeID = cd.ChuDeID,
                    NoiDung = r.row.NoiDung.Trim(),
                    LoaiCauHoi = loai,
                    MucDoKho = muc,
                    Diem = diem,
                    GiaiThichDapAn = string.IsNullOrWhiteSpace(r.row.GiaiThich) ? null : r.row.GiaiThich.Trim()
                };

                if (loai == LoaiCauHoi.TracNghiem)
                {
                    var opts = new[] { r.row.Da, r.row.Db, r.row.Dc, r.row.Dd };
                    var dung = r.row.Dung.Trim().ToUpperInvariant() switch
                    {
                        "A" => 0, "B" => 1, "C" => 2, "D" => 3, _ => -1
                    };
                    if (dung < 0 || opts.Any(string.IsNullOrWhiteSpace))
                    {
                        loi.Add($"Dòng {r.i}: câu trắc nghiệm cần đủ 4 đáp án và cột DapAnDung (A–D).");
                        continue;
                    }
                    for (var i = 0; i < 4; i++)
                        cau.DapAns.Add(new DapAn { ThuTu = i, NoiDung = opts[i].Trim(), LaDapAnDung = i == dung });
                }
                else if (loai == LoaiCauHoi.DungSai)
                {
                    // Cot DapAnA..D dung lam NOI DUNG y a-d, cot Y_a..Y_d la Dung/Sai
                    var opts = new[] { r.row.Da, r.row.Db, r.row.Dc, r.row.Dd };
                    var ys = new[] { r.row.Ya, r.row.Yb, r.row.Yc, r.row.Yd };
                    for (var i = 0; i < 4; i++)
                        cau.DapAns.Add(new DapAn
                        {
                            ThuTu = i,
                            NoiDung = string.IsNullOrWhiteSpace(opts[i]) ? $"Ý {((char)('a' + i))}" : opts[i].Trim(),
                            LaDapAnDung = string.Equals(ys[i].Trim(), "D", StringComparison.OrdinalIgnoreCase)
                        });
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(r.row.DapSo))
                    {
                        loi.Add($"Dòng {r.i}: câu trả lời ngắn cần cột DapSo.");
                        continue;
                    }
                    cau.DapAns.Add(new DapAn { ThuTu = 0, NoiDung = r.row.DapSo.Trim(), LaDapAnDung = true });
                }

                Them(cau);
                demThem++;
            }
            catch (Exception ex)
            {
                loi.Add($"Dòng {r.i}: {ex.Message}");
            }
        }
        return (demThem, loi.Count, loi);
    }
}
