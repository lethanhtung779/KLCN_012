using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Mappings;

namespace QLThiTN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MonHocController : ControllerBase
{
    private readonly QLThiTNDbContext _db;
    private readonly IMemoryCache? _cache;

    public MonHocController(QLThiTNDbContext db, IMemoryCache? cache = null)
    {
        _db = db;
        _cache = cache;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        const string cacheKey = "Cache_MonHoc_List";
        if (_cache != null && _cache.TryGetValue(cacheKey, out List<MonHocDto>? cached) && cached != null)
        {
            return Ok(cached);
        }

        var list = await _db.MonHocs
            .OrderBy(m => m.TenMon)
            .Select(m => new MonHocDto
            {
                MonHocID = m.MonHocID,
                TenMon = m.TenMon,
                SoChuDe = m.ChuDes.Count,
                SoCauHoi = m.ChuDes.Sum(c => c.CauHois.Count)
            })
            .ToListAsync();

        _cache?.Set(cacheKey, list, TimeSpan.FromMinutes(10));
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var mon = await _db.MonHocs
            .Include(m => m.ChuDes)
                .ThenInclude(c => c.CauHois)
            .FirstOrDefaultAsync(m => m.MonHocID == id);

        if (mon is null) return NotFound();
        return Ok(mon.ToDto());
    }

    [Authorize(Roles = "QuanTriVien")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] string tenMon)
    {
        if (string.IsNullOrWhiteSpace(tenMon)) return BadRequest("TenMon khong duoc de trong");

        var exists = await _db.MonHocs.AnyAsync(m => m.TenMon == tenMon);
        if (exists) return Conflict("Mon hoc da ton tai");

        var mon = new Entities.MonHoc { TenMon = tenMon };
        _db.MonHocs.Add(mon);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = mon.MonHocID }, mon.ToDto());
    }

    [Authorize(Roles = "QuanTriVien")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var mon = await _db.MonHocs.FindAsync(id);
        if (mon is null) return NotFound();

        var coLienQuan = await _db.ChuDes.AnyAsync(c => c.MonHocID == id)
            || await _db.DeThis.AnyAsync(d => d.MonHocID == id)
            || await _db.KhoaHocs.AnyAsync(k => k.MonHocID == id)
            || await _db.GiaoVien_MonHocs.AnyAsync(g => g.MonHocID == id);
        if (coLienQuan)
            return BadRequest(new { message = "Không thể xóa môn học này vì đã có chủ đề, câu hỏi, đề thi hoặc khóa học liên quan." });

        _db.MonHocs.Remove(mon);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}