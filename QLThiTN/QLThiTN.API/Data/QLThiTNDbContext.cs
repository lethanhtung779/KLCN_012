using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Entities;

namespace QLThiTN.API.Data;

public class QLThiTNDbContext : DbContext
{
    public QLThiTNDbContext(DbContextOptions<QLThiTNDbContext> options) : base(options)
    {
    }

    public DbSet<TaiKhoan> TaiKhoans => Set<TaiKhoan>();
    public DbSet<MonHoc> MonHocs => Set<MonHoc>();
    public DbSet<GiaoVien> GiaoViens => Set<GiaoVien>();
    public DbSet<QuanTriVien> QuanTriViens => Set<QuanTriVien>();
    public DbSet<HocVien> HocViens => Set<HocVien>();
    public DbSet<KhoaHoc> KhoaHocs => Set<KhoaHoc>();
    public DbSet<LopHoc> LopHocs => Set<LopHoc>();
    public DbSet<ChuDe> ChuDes => Set<ChuDe>();
    public DbSet<GiaoVien_MonHoc> GiaoVien_MonHocs => Set<GiaoVien_MonHoc>();
    public DbSet<CauHoi> CauHois => Set<CauHoi>();
    public DbSet<DapAn> DapAns => Set<DapAn>();
    public DbSet<HinhAnh> HinhAnhs => Set<HinhAnh>();
    public DbSet<HocVien_Lop> HocVien_Lops => Set<HocVien_Lop>();
    public DbSet<LichHoc> LichHocs => Set<LichHoc>();
    public DbSet<PhanCongGiangDay> PhanCongGiangDays => Set<PhanCongGiangDay>();
    public DbSet<DeThi> DeThis => Set<DeThi>();
    public DbSet<DeThi_CauHoi> DeThi_CauHois => Set<DeThi_CauHoi>();
    public DbSet<MaDeThi> MaDeThis => Set<MaDeThi>();
    public DbSet<MaDe_CauHoi> MaDe_CauHois => Set<MaDe_CauHoi>();
    public DbSet<MaDe_DapAn> MaDe_DapAns => Set<MaDe_DapAn>();
    public DbSet<DotThi> DotThis => Set<DotThi>();
    public DbSet<DotThi_LopHoc> DotThi_LopHocs => Set<DotThi_LopHoc>();
    public DbSet<DangKyDotThi> DangKyDotThis => Set<DangKyDotThi>();
    public DbSet<BaiLam> BaiLams => Set<BaiLam>();
    public DbSet<ChiTietBaiLam> ChiTietBaiLams => Set<ChiTietBaiLam>();
    public DbSet<LuaChonBaiLam> LuaChonBaiLams => Set<LuaChonBaiLam>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GiaoVien_MonHoc>(e =>
        {
            e.HasKey(x => new { x.GiaoVienID, x.MonHocID });
            e.HasOne(x => x.GiaoVien).WithMany().HasForeignKey(x => x.GiaoVienID);
            e.HasOne(x => x.MonHoc).WithMany().HasForeignKey(x => x.MonHocID);
        });

        modelBuilder.Entity<HocVien_Lop>(e =>
        {
            e.HasKey(x => new { x.HocVienID, x.LopHocID });
            e.HasOne(x => x.HocVien).WithMany().HasForeignKey(x => x.HocVienID);
            e.HasOne(x => x.LopHoc).WithMany().HasForeignKey(x => x.LopHocID);
        });

        modelBuilder.Entity<PhanCongGiangDay>(e =>
        {
            e.HasKey(x => new { x.LopHocID, x.MonHocID });
            e.HasOne(x => x.LopHoc).WithMany().HasForeignKey(x => x.LopHocID);
            e.HasOne(x => x.MonHoc).WithMany().HasForeignKey(x => x.MonHocID);
            e.HasOne(x => x.GiaoVien).WithMany().HasForeignKey(x => x.GiaoVienID);
        });

        modelBuilder.Entity<DeThi_CauHoi>(e =>
        {
            e.HasKey(x => new { x.DeThiID, x.CauHoiID });
            e.HasOne(x => x.DeThi).WithMany(x => x.DeThi_CauHois).HasForeignKey(x => x.DeThiID);
            e.HasOne(x => x.CauHoi).WithMany().HasForeignKey(x => x.CauHoiID);
        });

        modelBuilder.Entity<MaDe_CauHoi>(e =>
        {
            e.HasKey(x => new { x.MaDeID, x.CauHoiID });
            e.HasIndex(x => new { x.MaDeID, x.CauHoiID, x.ThuTu }).IsUnique();
            e.HasOne(x => x.MaDeThi).WithMany(x => x.MaDe_CauHois).HasForeignKey(x => x.MaDeID);
            e.HasOne(x => x.CauHoi).WithMany().HasForeignKey(x => x.CauHoiID);
        });

        modelBuilder.Entity<MaDe_DapAn>(e =>
        {
            e.HasKey(x => new { x.MaDeID, x.CauHoiID, x.DapAnID });
            e.HasIndex(x => new { x.MaDeID, x.CauHoiID, x.ThuTu }).IsUnique();
            e.HasOne(x => x.MaDeThi).WithMany().HasForeignKey(x => x.MaDeID);
            e.HasOne(x => x.CauHoi).WithMany().HasForeignKey(x => x.CauHoiID);
            e.HasOne(x => x.DapAn).WithMany().HasForeignKey(x => x.DapAnID);
        });

        modelBuilder.Entity<DotThi_LopHoc>(e =>
        {
            e.HasKey(x => new { x.DotThiID, x.LopHocID });
            e.HasOne(x => x.DotThi).WithMany().HasForeignKey(x => x.DotThiID);
            e.HasOne(x => x.LopHoc).WithMany().HasForeignKey(x => x.LopHocID);
        });

        modelBuilder.Entity<DangKyDotThi>(e =>
        {
            e.HasIndex(x => new { x.HocVienID, x.DotThiID }).IsUnique();
            e.HasOne(x => x.HocVien).WithMany().HasForeignKey(x => x.HocVienID);
            e.HasOne(x => x.DotThi).WithMany().HasForeignKey(x => x.DotThiID);
        });

        modelBuilder.Entity<BaiLam>(e =>
        {
            e.HasIndex(x => new { x.HocVienID, x.DotThiID, x.LanThi }).IsUnique();
            e.HasOne(x => x.HocVien).WithMany().HasForeignKey(x => x.HocVienID);
            e.HasOne(x => x.DotThi).WithMany().HasForeignKey(x => x.DotThiID);
            e.HasOne(x => x.MaDeThi).WithMany().HasForeignKey(x => x.MaDeID);
        });

        modelBuilder.Entity<ChiTietBaiLam>(e =>
        {
            e.HasIndex(x => new { x.BaiLamID, x.CauHoiID }).IsUnique();
            e.HasOne(x => x.BaiLam).WithMany().HasForeignKey(x => x.BaiLamID);
            e.HasOne(x => x.CauHoi).WithMany().HasForeignKey(x => x.CauHoiID);
        });

        modelBuilder.Entity<LuaChonBaiLam>(e =>
        {
            e.HasIndex(x => new { x.ChiTietID, x.DapAnID })
                .IsUnique()
                .HasFilter("DapAnID IS NOT NULL");
            e.HasIndex(x => x.ChiTietID)
                .IsUnique()
                .HasFilter("DapAnID IS NULL");
            e.HasOne(x => x.ChiTietBaiLam).WithMany(x => x.LuaChonBaiLams).HasForeignKey(x => x.ChiTietID);
            e.HasOne(x => x.DapAn).WithMany().HasForeignKey(x => x.DapAnID);
        });

        modelBuilder.Entity<TaiKhoan>(e =>
        {
            e.HasIndex(x => x.TenDangNhap).IsUnique();
            e.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<CauHoi>(e =>
        {
            e.HasOne(x => x.ChuDe).WithMany(x => x.CauHois).HasForeignKey(x => x.ChuDeID);
            e.HasOne(x => x.GiaoVien).WithMany(x => x.CauHois).HasForeignKey(x => x.GiaoVienID);
        });
    }
}