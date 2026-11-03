using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;

namespace QLThiTN.API.Services;

/// <summary>Goi tai khoan hoc vien va quyen lam de thi.</summary>
public static class GoiThi
{
    public const string MienPhi = "MienPhi";
    public const string DongPhi = "DongPhi";
    public const string VangLai = "VangLai";
    public const string TrungTam = "TrungTam";

    public const string ThongBao =
        "Đề thi này chỉ dành cho học viên trung tâm hoặc tài khoản có VIP cá nhân. Tài khoản miễn phí chỉ được làm các đề miễn phí.";

    public sealed record QuyenHocVien(bool LaHocVienTrungTam, bool LaVipCaNhan)
    {
        public bool LaVip => LaHocVienTrungTam || LaVipCaNhan;
        public string LoaiGoiHieuLuc => LaVip ? DongPhi : MienPhi;
        public string NguonVip => LaVipCaNhan && LaHocVienTrungTam ? "CaNhanVaTrungTam"
            : LaVipCaNhan ? "CaNhan"
            : LaHocVienTrungTam ? "TrungTam"
            : "KhongCo";
    }

    public const string SchemaSql = @"
IF COL_LENGTH('HocVien', 'VipExpiresUtc') IS NULL
    ALTER TABLE HocVien ADD VipExpiresUtc datetime2 NULL;
IF COL_LENGTH('HocVien', 'LoaiGoi') IS NULL
    ALTER TABLE HocVien ADD LoaiGoi NVARCHAR(20) NOT NULL CONSTRAINT DF_HocVien_LoaiGoi DEFAULT N'MienPhi';
IF COL_LENGTH('HocVien', 'NguonTaiKhoan') IS NULL
BEGIN
    ALTER TABLE HocVien ADD NguonTaiKhoan NVARCHAR(20) NOT NULL
        CONSTRAINT DF_HocVien_NguonTaiKhoan DEFAULT N'TrungTam';
    EXEC(N'UPDATE HocVien SET NguonTaiKhoan=N''VangLai'' WHERE LoaiHocVien=N''TuDo''');
    IF OBJECT_ID(N'dbo.DangKyXacMinhEmail', N'U') IS NOT NULL
        EXEC(N'UPDATE hv SET NguonTaiKhoan=N''VangLai'' FROM HocVien hv JOIN TaiKhoan tk ON tk.TaiKhoanID=hv.TaiKhoanID WHERE EXISTS(SELECT 1 FROM DangKyXacMinhEmail d WHERE d.Email=tk.Email AND d.DaSuDung=1)');
    EXEC(N'DELETE hvl FROM HocVien_Lop hvl JOIN HocVien hv ON hv.HocVienID=hvl.HocVienID WHERE hv.NguonTaiKhoan=N''VangLai''');
END;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.HocVien') AND name=N'CK_HocVien_NguonTaiKhoan')
    EXEC(N'ALTER TABLE HocVien WITH CHECK ADD CONSTRAINT CK_HocVien_NguonTaiKhoan CHECK (NguonTaiKhoan IN (N''VangLai'',N''TrungTam''))');
EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_HocVienLop_ChiHocVienTrungTam ON dbo.HocVien_Lop AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.HocVien hv ON hv.HocVienID=i.HocVienID WHERE hv.NguonTaiKhoan<>N''TrungTam'')
        THROW 51020,N''Tài khoản học viên vãng lai không được xếp vào lớp trung tâm. Hãy tạo tài khoản học viên trung tâm mới.'',1;
END');
IF COL_LENGTH('DeThi', 'LaMienPhi') IS NULL
    ALTER TABLE DeThi ADD LaMienPhi BIT NOT NULL CONSTRAINT DF_DeThi_LaMienPhi DEFAULT 0;
";

    /// <summary>Kiem tra quyen duoc lam theo DotThi (Uu tien so 1 theo dung phan quyen ky thi).</summary>
    public static async Task<bool?> DuocLamDotThiAsync(QLThiTNDbContext db, int dotThiId, int? hocVienId)
    {
        var dot = await db.DotThis
            .Where(d => d.DotThiID == dotThiId)
            .Select(d => new { d.LaMienPhi, d.DeThiID })
            .FirstOrDefaultAsync();

        if (dot is null) return null;
        if (dot.LaMienPhi) return true;
        if (hocVienId is not > 0) return false;

        return (await LayQuyenAsync(db, hocVienId.Value))?.LaVip == true;
    }

    /// <summary>null = khong co de; true = duoc lam; false = de/dot dong phi va tai khoan chua VIP.</summary>
    public static async Task<bool?> DuocLamAsync(QLThiTNDbContext db, int deThiId, int? hocVienId, int? dotThiId = null)
    {
        if (dotThiId.HasValue && dotThiId.Value > 0)
        {
            return await DuocLamDotThiAsync(db, dotThiId.Value, hocVienId);
        }

        // Kiem tra xem de thi co dot thi nao dang mo va mien phi khong
        var dotMienPhi = await db.DotThis
            .Where(dt => dt.DeThiID == deThiId && dt.TrangThai == "DangMo")
            .Select(dt => (bool?)dt.LaMienPhi)
            .FirstOrDefaultAsync();
        if (dotMienPhi == true) return true;

        var laMienPhi = await db.DeThis
            .Where(d => d.DeThiID == deThiId)
            .Select(d => (bool?)d.LaMienPhi)
            .FirstOrDefaultAsync();

        if (laMienPhi is null) return null;
        if (dotMienPhi is null && laMienPhi == true) return true;
        if (hocVienId is not > 0) return false;

        return (await LayQuyenAsync(db, hocVienId.Value))?.LaVip == true;
    }

    /// <summary>
    /// Tài khoản trung tâm do quản trị viên tạo luôn có VIP trung tâm. LoaiGoi chỉ lưu VIP cá nhân
    /// của tài khoản vãng lai và không phụ thuộc vào việc tham gia lớp.
    /// </summary>
    public static async Task<QuyenHocVien?> LayQuyenAsync(QLThiTNDbContext db, int hocVienId)
    {
        return await db.HocViens
            .Where(h => h.HocVienID == hocVienId)
            .Select(h => new QuyenHocVien(
                h.NguonTaiKhoan == TrungTam,
                h.LoaiGoi == DongPhi && (h.VipExpiresUtc == null || h.VipExpiresUtc > DateTime.UtcNow)))
            .FirstOrDefaultAsync();
    }
}
