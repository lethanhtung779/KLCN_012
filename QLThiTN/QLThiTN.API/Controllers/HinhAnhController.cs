using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;

namespace QLThiTN.API.Controllers;

/// <summary>
/// Phuc vu file anh cua cau hoi. Duong dan trong DB la duong dan tuong doi
/// ("ab/abc123.png") so voi thu muc goc cau hinh o "HinhAnhRoot".
/// </summary>
[ApiController]
[Route("api/hinhanh")]
public class HinhAnhController : ControllerBase
{
    private static readonly string[] DuongDanChoPhep =
        { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };

    private readonly QLThiTNDbContext _db;
    private readonly string _root;

    public HinhAnhController(QLThiTNDbContext db, IConfiguration config)
    {
        _db = db;
        _root = Path.GetFullPath(
            config["HinhAnhRoot"] ?? @"D:\KLCN\tach_cau_hoi\tai_nguyen");
    }

    /// <summary>Tra ve danh sach duong dan cua mot cau hoi (de canh bao file mat).</summary>
    [HttpGet("cauhoi/{cauHoiId:int}")]
    public async Task<IActionResult> ByQuestion(int cauHoiId)
    {
        var rows = await _db.HinhAnhs
            .Where(h => h.CauHoiID == cauHoiId)
            .OrderBy(h => h.ThuTu)
            .Select(h => new { h.HinhAnhID, h.DuongDan, h.MoTa, h.ThuTu })
            .ToListAsync();

        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var row = await _db.HinhAnhs
            .Where(h => h.HinhAnhID == id)
            .Select(h => new { h.DuongDan })
            .FirstOrDefaultAsync();

        if (row is null) return NotFound();

        // Chuan hoa "/" va "..", roi kiem tra file van nam trong thu muc goc.
        var relative = row.DuongDan.Replace('\\', '/').TrimStart('/');
        var full = Path.GetFullPath(Path.Combine(_root, relative));

        if (!full.StartsWith(_root + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Duong dan anh khong hop le.");
        }

        var ext = Path.GetExtension(full).ToLowerInvariant();
        if (!DuongDanChoPhep.Contains(ext))
        {
            return BadRequest("Dinh dang anh khong ho tro.");
        }

        if (!System.IO.File.Exists(full))
        {
            return NotFound("Khong tim thay file anh tren dia.");
        }

        var mime = ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/bmp"
        };

        // Duong dan trong DB la hash noi dung nen cache lau duoc.
        return PhysicalFile(full, mime);
    }
}
