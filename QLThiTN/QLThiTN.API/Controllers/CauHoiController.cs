using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Mappings;

namespace QLThiTN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CauHoiController : ControllerBase
{
    private readonly QLThiTNDbContext _db;

    public CauHoiController(QLThiTNDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? monHocId,
        [FromQuery] int? chuDeId,
        [FromQuery] string? loai,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var query = _db.CauHois
            .Include(c => c.ChuDe)
                .ThenInclude(cd => cd!.MonHoc)
            .Include(c => c.DapAns)
            .AsQueryable();

        if (monHocId.HasValue)
            query = query.Where(c => c.ChuDe!.MonHocID == monHocId.Value);
        if (chuDeId.HasValue)
            query = query.Where(c => c.ChuDeID == chuDeId.Value);
        if (!string.IsNullOrWhiteSpace(loai))
            query = query.Where(c => c.LoaiCauHoi == loai);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.CauHoiID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items.Select(c => c.ToDto())
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var cauHoi = await _db.CauHois
            .Include(c => c.ChuDe)
                .ThenInclude(cd => cd!.MonHoc)
            .Include(c => c.DapAns)
            .FirstOrDefaultAsync(c => c.CauHoiID == id);

        if (cauHoi is null) return NotFound();
        return Ok(cauHoi.ToDto());
    }
}