using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;
using QLThiTN.API.Services;

namespace QLThiTN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DotThiController : ControllerBase
{
    private readonly QLThiTNDbContext _db;

    public DotThiController(QLThiTNDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? hocVienId)
    {
        hocVienId = User.StudentId() > 0 ? User.StudentId() : null;
        var list = await _db.DotThis
            .Include(d => d.DeThi)
                .ThenInclude(d => d!.MonHoc)
            .OrderBy(d => d.ThoiGianMoCong)
            .ToListAsync();

        var counts = await _db.DangKyDotThis
            .Where(k => k.TrangThai != "DaHuy")
            .GroupBy(k => k.DotThiID)
            .Select(g => new { DotThiID = g.Key, SoLuong = g.Count() })
            .ToDictionaryAsync(x => x.DotThiID, x => x.SoLuong);

        var myRegs = hocVienId.HasValue
            ? (await _db.DangKyDotThis
                .Where(k => k.HocVienID == hocVienId.Value && k.TrangThai != "DaHuy")
                .Select(k => k.DotThiID)
                .ToListAsync()).ToHashSet()
            : new HashSet<int>();

        return Ok(list.Select(d =>
        {
            var dto = ToDto(d);
            dto.SoDangKy = counts.GetValueOrDefault(d.DotThiID);
            dto.SoChoConLai = d.GioiHanSoLuong.HasValue
                ? d.GioiHanSoLuong.Value - dto.SoDangKy
                : null;
            dto.DaDangKy = myRegs.Contains(d.DotThiID);
            return dto;
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dot = await _db.DotThis
            .Include(d => d.DeThi)
                .ThenInclude(d => d!.MonHoc)
            .FirstOrDefaultAsync(d => d.DotThiID == id);

        if (dot is null) return NotFound();
        return Ok(ToDto(dot));
    }

    [Authorize(Roles = "QuanTriVien")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DotThiDto request)
    {
        var de = await _db.DeThis.FindAsync(request.DeThiID);
        if (de is null) return BadRequest(new { message = "DeThi khong ton tai" });

        var dot = new DotThi
        {
            DeThiID = request.DeThiID,
            TenDotThi = request.TenDotThi,
            ThoiGianMoCong = request.ThoiGianMoCong,
            ThoiGianDongCong = request.ThoiGianDongCong,
            ThoiLuongLamBai = request.ThoiLuongLamBai,
            GioiHanSoLuong = request.GioiHanSoLuong,
            CongBoDiemSom = request.CongBoDiemSom,
            TrangThai = request.TrangThai,
            PhamVi = string.IsNullOrWhiteSpace(request.PhamVi) ? "ToanTruong" : request.PhamVi,
            SoLanThiToiDa = request.SoLanThiToiDa > 0 ? request.SoLanThiToiDa : 1
        };
        _db.DotThis.Add(dot);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = dot.DotThiID }, dot);
    }

    [Authorize(Roles = "QuanTriVien")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var dot = await _db.DotThis.FindAsync(id);
        if (dot is null) return NotFound();

        _db.DotThis.Remove(dot);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "HocVien")]
    [HttpPost("{id:int}/dangky")]
    public async Task<IActionResult> DangKy(int id, [FromBody] DangKyDotThiRequestDto request)
    {
        if (request.HocVienID != User.StudentId()) return Forbid();
        await using var transaction = await ExamTransaction.BeginAsync(_db, $"register:{id}");
        var dot = await _db.DotThis.FirstOrDefaultAsync(d => d.DotThiID == id);
        if (dot is null) return NotFound(new { message = "Khong tim thay dot thi" });

        if (dot.TrangThai == "DaDong" || dot.ThoiGianDongCong < DateTime.Now)
            return BadRequest(new { message = "Dot thi da dong, khong the dang ky" });

        if (dot.PhamVi == "TheoLop")
        {
            var thuocLop = await _db.DotThi_LopHocs
                .AnyAsync(dtl => dtl.DotThiID == dot.DotThiID &&
                    _db.HocVien_Lops.Any(hvl => hvl.LopHocID == dtl.LopHocID && hvl.HocVienID == request.HocVienID && hvl.TrangThai == "DangHoc"));
            if (!thuocLop)
                return StatusCode(403, new { message = "Đợt thi chỉ dành cho các lớp học được phân công. Bạn không thuộc danh sách dự thi." });
        }

        var hv = await _db.HocViens.FindAsync(request.HocVienID);
        if (hv is null) return BadRequest(new { message = "Hoc vien khong ton tai" });
        if (await GoiThi.DuocLamAsync(_db, dot.DeThiID, hv.HocVienID) == false)
            return StatusCode(403, new { message = GoiThi.ThongBao });

        var existing = await _db.DangKyDotThis
            .FirstOrDefaultAsync(k => k.DotThiID == id && k.HocVienID == request.HocVienID);

        if (existing is not null && existing.TrangThai != "DaHuy")
            return Conflict(new { message = "Ban da dang ky dot thi nay" });

        if (dot.GioiHanSoLuong.HasValue)
        {
            var soDangKy = await _db.DangKyDotThis
                .CountAsync(k => k.DotThiID == id && k.TrangThai != "DaHuy");
            if (soDangKy >= dot.GioiHanSoLuong.Value)
                return Conflict(new { message = "Dot thi da du so luong thi sinh" });
        }

        if (existing is not null)
        {
            existing.TrangThai = "DaDangKy";
            existing.NgayDangKy = DateTime.Now;
        }
        else
        {
            _db.DangKyDotThis.Add(new DangKyDotThi
            {
                HocVienID = request.HocVienID,
                DotThiID = id,
                TrangThai = "DaDangKy",
                NgayDangKy = DateTime.Now
            });
        }
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new { message = "Dang ky dot thi thanh cong" });
    }

    [Authorize(Roles = "HocVien")]
    [HttpDelete("{id:int}/dangky/{hocVienId:int}")]
    public async Task<IActionResult> HuyDangKy(int id, int hocVienId)
    {
        if (hocVienId != User.StudentId()) return Forbid();
        var dk = await _db.DangKyDotThis
            .FirstOrDefaultAsync(k => k.DotThiID == id && k.HocVienID == hocVienId && k.TrangThai != "DaHuy");
        if (dk is null) return NotFound(new { message = "Ban chua dang ky dot thi nay" });

        dk.TrangThai = "DaHuy";
        await _db.SaveChangesAsync();
        return Ok(new { message = "Da huy dang ky dot thi" });
    }

    private static DotThiDto ToDto(DotThi d) => new()
    {
        DotThiID = d.DotThiID,
        DeThiID = d.DeThiID,
        TenDotThi = d.TenDotThi,
        TenDe = d.DeThi?.TenDe ?? string.Empty,
        TenMon = d.DeThi?.MonHoc?.TenMon ?? string.Empty,
        ThoiGianMoCong = d.ThoiGianMoCong,
        ThoiGianDongCong = d.ThoiGianDongCong,
        ThoiLuongLamBai = d.ThoiLuongLamBai,
        GioiHanSoLuong = d.GioiHanSoLuong,
        CongBoDiemSom = d.CongBoDiemSom,
        TrangThai = d.TrangThai,
        PhamVi = d.PhamVi,
        SoLanThiToiDa = d.SoLanThiToiDa
    };
}