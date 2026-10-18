using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
namespace QLThiTN.API.Controllers;

/// <summary>Thoi khoa bieu (LichHoc) theo lop hoc — phuc vu website hoc vien.</summary>
[Authorize(Roles = "HocVien")]
[ApiController]
[Route("api/[controller]")]
public class LichHocController : ControllerBase
{
    private readonly QLThiTNDbContext _db;

    public LichHocController(QLThiTNDbContext db) => _db = db;

    /// <summary>
    /// Lay lop da dang ky + buoi hoc cua hoc vien.
    /// Chi lay lop co trang thai DangHoc hoac DaKetThuc (khong lay ThoiHoc).
    /// Query: lopHocId (loc 1 lop), tuNgay/denNgay (loc buoi hoc giao voi khoang thoi gian).
    /// </summary>
    [HttpGet("hocvien/{hocVienId:int}")]
    public async Task<ActionResult<HocVienThoiKhoaBieuDto>> GetByHocVien(
        int hocVienId,
        [FromQuery] int? lopHocId = null,
        [FromQuery] DateTime? tuNgay = null,
        [FromQuery] DateTime? denNgay = null)
    {
        if (hocVienId != User.StudentId()) return Forbid();
        if (!await _db.HocViens.AnyAsync(h => h.HocVienID == hocVienId))
            return NotFound(new { Message = "Không tìm thấy học viên." });

        var enrollments = await LayDangKyLop(hocVienId);
        if (enrollments.Count == 0)
            return Ok(new HocVienThoiKhoaBieuDto());

        var lopHocs = enrollments
            .Where(e => e.LopHoc != null)
            .Select(e => new LopHocSummaryDto
            {
                LopHocID = e.LopHocID,
                TenLop = e.LopHoc!.TenLop,
                TenKhoaHoc = e.LopHoc.KhoaHoc?.TenKhoaHoc ?? "",
                TenMon = e.LopHoc.KhoaHoc?.MonHoc?.TenMon ?? "",
                TrangThai = e.TrangThai,
                TrangThaiText = HienThiTrangThaiLop(e.TrangThai)
            })
            .OrderBy(x => x.TenLop)
            .ToList();

        var lopIds = enrollments.Select(e => e.LopHocID).Distinct().ToList();

        if (lopHocId.HasValue)
        {
            if (!lopIds.Contains(lopHocId.Value))
                return BadRequest(new { Message = "Học viên không thuộc lớp này hoặc đã thôi học." });
            lopIds = [lopHocId.Value];
        }

        var tu = tuNgay?.Date;
        var den = denNgay?.Date.AddDays(1).AddTicks(-1);

        var phanCongList = await LayPhanCong(lopIds);
        var buoiHoc = await LayBuoiHoc(lopIds, tu, den, phanCongList);

        return Ok(new HocVienThoiKhoaBieuDto
        {
            LopHocs = lopHocs,
            BuoiHoc = buoiHoc,
            PhanCongGiangDay = phanCongList
        });
    }

    /// <summary>Buoi hoc cua mot lop (chi tra ve neu hoc vien thuoc lop).</summary>
    [HttpGet("lop/{lopHocId:int}")]
    public async Task<ActionResult<List<LichHocItemDto>>> GetByLop(
        int lopHocId,
        [FromQuery] int hocVienId,
        [FromQuery] DateTime? tuNgay = null,
        [FromQuery] DateTime? denNgay = null)
    {
        if (hocVienId <= 0)
            return BadRequest(new { Message = "Thiếu hocVienId." });

        var thuocLop = await _db.HocVien_Lops.AnyAsync(h =>
            h.HocVienID == hocVienId && h.LopHocID == lopHocId && h.TrangThai != "ThoiHoc");
        if (!thuocLop)
            return NotFound(new { Message = "Học viên không thuộc lớp này." });

        if (!await _db.LopHocs.AnyAsync(l => l.LopHocID == lopHocId))
            return NotFound(new { Message = "Không tìm thấy lớp học." });

        var tu = tuNgay?.Date;
        var den = denNgay?.Date.AddDays(1).AddTicks(-1);
        var phanCongList = await LayPhanCong([lopHocId]);
        var items = await LayBuoiHoc([lopHocId], tu, den, phanCongList);
        return Ok(items);
    }

    private async Task<List<PhanCongGiangDayItemDto>> LayPhanCong(List<int> lopIds)
    {
        if (lopIds.Count == 0) return [];
        return await _db.PhanCongGiangDays
            .Where(p => lopIds.Contains(p.LopHocID))
            .Include(p => p.MonHoc)
            .Include(p => p.GiaoVien)
            .OrderBy(p => p.LopHocID).ThenBy(p => p.MonHoc!.TenMon)
            .Select(p => new PhanCongGiangDayItemDto
            {
                LopHocID = p.LopHocID,
                MonHocID = p.MonHocID,
                TenMon = p.MonHoc != null ? p.MonHoc.TenMon : "",
                TenGiaoVien = p.GiaoVien != null ? p.GiaoVien.HoTen : ""
            })
            .ToListAsync();
    }

    private static string TomTatPhanCong(IEnumerable<PhanCongGiangDayItemDto> items) =>
        string.Join("; ", items
            .Where(x => !string.IsNullOrWhiteSpace(x.TenMon))
            .Select(x => $"{x.TenMon}: {x.TenGiaoVien}"));

    private async Task<List<Entities.HocVien_Lop>> LayDangKyLop(int hocVienId) =>
        await _db.HocVien_Lops
            .Where(h => h.HocVienID == hocVienId && h.TrangThai != "ThoiHoc")
            .Include(h => h.LopHoc)
                .ThenInclude(l => l!.KhoaHoc)
                    .ThenInclude(k => k!.MonHoc)
            .ToListAsync();

    private async Task<List<LichHocItemDto>> LayBuoiHoc(
        List<int> lopIds,
        DateTime? tuNgay,
        DateTime? denNgay,
        List<PhanCongGiangDayItemDto> phanCongList)
    {
        if (lopIds.Count == 0) return [];

        var gvTheoLopMon = phanCongList.ToDictionary(
            p => (p.LopHocID, p.MonHocID),
            p => p.TenGiaoVien);
        var tomTatTheoLop = phanCongList
            .GroupBy(p => p.LopHocID)
            .ToDictionary(g => g.Key, g => TomTatPhanCong(g));

        var query = _db.LichHocs
            .Where(l => lopIds.Contains(l.LopHocID));

        if (tuNgay.HasValue)
            query = query.Where(l => l.ThoiGianKetThuc >= tuNgay.Value);
        if (denNgay.HasValue)
            query = query.Where(l => l.ThoiGianBatDau <= denNgay.Value);

        var buoiHoc = await query
            .Include(l => l.LopHoc)
                .ThenInclude(lh => lh!.KhoaHoc)
                    .ThenInclude(kh => kh!.MonHoc)
            .OrderBy(l => l.ThoiGianBatDau)
            .ToListAsync();

        return buoiHoc.Select(l =>
        {
            var monId = l.LopHoc?.KhoaHoc?.MonHocID ?? 0;
            gvTheoLopMon.TryGetValue((l.LopHocID, monId), out var tenGv);
            tomTatTheoLop.TryGetValue(l.LopHocID, out var tomTat);
            var phanCongStr = string.IsNullOrWhiteSpace(tomTat) ? null : tomTat;
            return new LichHocItemDto
            {
                LichHocID = l.LichHocID,
                LopHocID = l.LopHocID,
                TenLop = l.LopHoc?.TenLop ?? "",
                TenKhoaHoc = l.LopHoc?.KhoaHoc?.TenKhoaHoc ?? "",
                TenMon = l.LopHoc?.KhoaHoc?.MonHoc?.TenMon ?? "",
                NoiDung = l.NoiDung,
                ThoiGianBatDau = l.ThoiGianBatDau,
                ThoiGianKetThuc = l.ThoiGianKetThuc,
                DiaDiem = l.DiaDiem,
                TenGiaoVien = string.IsNullOrWhiteSpace(tenGv) ? null : tenGv,
                PhanCongTomTat = phanCongStr
            };
        }).ToList();
    }

    private static string HienThiTrangThaiLop(string code) => code switch
    {
        "DangHoc" => "Đang học",
        "DaKetThuc" => "Đã kết thúc",
        "ThoiHoc" => "Thôi học",
        _ => code
    };
}
