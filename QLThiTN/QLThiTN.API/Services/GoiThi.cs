using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;

namespace QLThiTN.API.Services;

/// <summary>Goi tai khoan hoc vien va quyen lam de thi.</summary>
public static class GoiThi
{
    public const string MienPhi = "MienPhi";
    public const string DongPhi = "DongPhi";

    public const string ThongBao =
        "Đề thi này chỉ dành cho tài khoản đã đóng phí (VIP). Tài khoản miễn phí chỉ được làm các đề miễn phí.";

    public const string SchemaSql = @"
IF COL_LENGTH('HocVien', 'LoaiGoi') IS NULL
    ALTER TABLE HocVien ADD LoaiGoi NVARCHAR(20) NOT NULL CONSTRAINT DF_HocVien_LoaiGoi DEFAULT N'MienPhi';
IF COL_LENGTH('DeThi', 'LaMienPhi') IS NULL
    ALTER TABLE DeThi ADD LaMienPhi BIT NOT NULL CONSTRAINT DF_DeThi_LaMienPhi DEFAULT 0;
";

    /// <summary>null = khong co de; true = duoc lam; false = de dong phi va tai khoan chua VIP.</summary>
    public static async Task<bool?> DuocLamAsync(QLThiTNDbContext db, int deThiId, int? hocVienId)
    {
        var laMienPhi = await db.DeThis
            .Where(d => d.DeThiID == deThiId)
            .Select(d => (bool?)d.LaMienPhi)
            .FirstOrDefaultAsync();

        if (laMienPhi is null)
            return null;
        if (laMienPhi == true)
            return true;
        if (hocVienId is not > 0)
            return false;

        var goi = await db.HocViens
            .Where(h => h.HocVienID == hocVienId)
            .Select(h => h.LoaiGoi)
            .FirstOrDefaultAsync();

        return goi == DongPhi;
    }
}
