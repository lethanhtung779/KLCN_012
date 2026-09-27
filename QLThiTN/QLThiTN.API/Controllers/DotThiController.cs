using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;

namespace QLThiTN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DotThiController : ControllerBase
{
    private readonly QLThiTNDbContext _db;

    public DotThiController(QLThiTNDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.DotThis
            .Include(d => d.DeThi)
                .ThenInclude(d => d!.MonHoc)
            .OrderBy(d => d.ThoiGianMoCong)
            .ToListAsync();

        return Ok(list.Select(ToDto));
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var dot = await _db.DotThis.FindAsync(id);
        if (dot is null) return NotFound();

        _db.DotThis.Remove(dot);
        await _db.SaveChangesAsync();
        return NoContent();
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