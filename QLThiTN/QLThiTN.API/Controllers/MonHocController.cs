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

    public MonHocController(QLThiTNDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.MonHocs
            .Include(m => m.ChuDes)
                .ThenInclude(c => c.CauHois)
            .OrderBy(m => m.TenMon)
            .ToListAsync();

        return Ok(list.Select(m => m.ToDto()));
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var mon = await _db.MonHocs.FindAsync(id);
        if (mon is null) return NotFound();

        _db.MonHocs.Remove(mon);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}