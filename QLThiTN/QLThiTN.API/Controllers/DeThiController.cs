using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLThiTN.API.Data;
using QLThiTN.API.DTOs;
using QLThiTN.API.Entities;

namespace QLThiTN.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeThiController : ControllerBase
{
    private readonly QLThiTNDbContext _db;

    public DeThiController(QLThiTNDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? monHocId,
        [FromQuery] string? loaiDe)
    {
        var query = _db.DeThis
            .Include(d => d.MonHoc)
            .Include(d => d.DotThis)
            .OrderByDescending(d => d.DeThiID)
            .AsQueryable();

        if (monHocId.HasValue)
            query = query.Where(d => d.MonHocID == monHocId.Value);
        if (!string.IsNullOrWhiteSpace(loaiDe))
            query = query.Where(d => d.LoaiDe == loaiDe);

        var list = await query.ToListAsync();
        return Ok(list.Select(ToListItemDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var de = await _db.DeThis
            .Include(d => d.MonHoc)
            .Include(d => d.DotThis)
            .FirstOrDefaultAsync(d => d.DeThiID == id);

        if (de is null) return NotFound();
        return Ok(ToListItemDto(de));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeThiRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.TenDe))
            return BadRequest(new { message = "TenDe khong duoc de trong" });
        if (request.SoLuongCauHoi < 1)
            return BadRequest(new { message = "SoLuongCauHoi phai >= 1" });

        var chuDes = await _db.ChuDes
            .Where(c => c.MonHocID == request.MonHocID)
            .Select(c => c.ChuDeID)
            .ToListAsync();

        if (chuDes.Count == 0)
            return BadRequest(new { message = "Mon hoc chua co chu de" });

        var pool = await _db.CauHois
            .Where(c => chuDes.Contains(c.ChuDeID) && c.TrangThai == "HoatDong")
            .Select(c => c.CauHoiID)
            .ToListAsync();

        if (pool.Count < request.SoLuongCauHoi)
            return BadRequest(new { message = $"Khong du cau hoi (co {pool.Count}, can {request.SoLuongCauHoi})" });

        var random = Random.Shared;
        var chosen = pool.OrderBy(_ => random.Next()).Take(request.SoLuongCauHoi).ToList();
        chosen.Sort();

        var de = new DeThi
        {
            TenDe = request.TenDe,
            MonHocID = request.MonHocID,
            GiaoVienID = request.GiaoVienID,
            LoaiDe = request.LoaiDe,
            SoLuongCauHoi = chosen.Count,
            HinhThucTao = "TuDong"
        };
        _db.DeThis.Add(de);
        for (int i = 0; i < chosen.Count; i++)
        {
            _db.DeThi_CauHois.Add(new DeThi_CauHoi
            {
                DeThi = de,
                CauHoiID = chosen[i],
                ThuTu = i + 1
            });
        }
        await _db.SaveChangesAsync();

        var created = await _db.DeThis
            .Include(d => d.MonHoc)
            .Include(d => d.DotThis)
            .FirstAsync(d => d.DeThiID == de.DeThiID);
        return CreatedAtAction(nameof(GetById), new { id = de.DeThiID }, ToListItemDto(created));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var de = await _db.DeThis.FindAsync(id);
        if (de is null) return NotFound();

        _db.DeThis.Remove(de);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("{id:int}/cauhoi")]
    public async Task<IActionResult> GetQuestions(int id)
    {
        var qs = await _db.DeThi_CauHois
            .Where(x => x.DeThiID == id)
            .OrderBy(x => x.ThuTu)
            .Include(x => x.CauHoi)
                .ThenInclude(c => c!.DapAns)
                    .ThenInclude(d => d.HinhAnhs)
            .Include(x => x.CauHoi)
                .ThenInclude(c => c!.HinhAnhs)
            .ToListAsync();

        if (qs.Count == 0) return NotFound();

        return Ok(qs.Select(q => new QuestionForDoingDto
        {
            CauHoiID = q.CauHoiID,
            OrderIndex = q.ThuTu,
            NoiDung = q.CauHoi!.NoiDung,
            LoaiCauHoi = q.CauHoi.LoaiCauHoi,
            LoaiCauHoiText = LoaiDeTextHelper.LoaiCauHoiText(q.CauHoi.LoaiCauHoi),
            HinhAnh = ToHinhAnhDto(q.CauHoi.HinhAnhs),
            Options = q.CauHoi.LoaiCauHoi == "TraLoiNgan"
                ? new List<OptionForDoingDto>()
                : q.CauHoi.DapAns
                    .OrderBy(d => d.ThuTu)
                    .Select(d => new OptionForDoingDto
                    {
                        DapAnID = d.DapAnID,
                        ThuTu = d.ThuTu,
                        NoiDung = d.NoiDung,
                        Label = LabelOf(d.ThuTu),
                        HinhAnh = ToHinhAnhDto(d.HinhAnhs)
                    }).ToList()
        }));
    }

    [HttpPost("{id:int}/nopbai")]
    public async Task<IActionResult> Submit(int id, [FromBody] SubmitExamRequestDto request)
    {
        var items = await _db.DeThi_CauHois
            .Where(x => x.DeThiID == id)
            .OrderBy(x => x.ThuTu)
            .Include(x => x.CauHoi)
                .ThenInclude(c => c!.DapAns)
                    .ThenInclude(d => d.HinhAnhs)
            .Include(x => x.CauHoi)
                .ThenInclude(c => c!.HinhAnhs)
            .ToListAsync();

        if (items.Count == 0) return NotFound();

        var correctMap = (await _db.DapAns
            .Where(d => d.LaDapAnDung)
            .OrderBy(d => d.ThuTu)
            .ToListAsync())
            .GroupBy(d => d.CauHoiID)
            .ToDictionary(g => g.Key, g => g.First());

        var answerMap = request.Answers.ToDictionary(a => a.CauHoiID);

        var results = new List<QuestionResultDto>();
        int correct = 0;

        foreach (var item in items)
        {
            var cau = item.CauHoi!;
            var answer = answerMap.GetValueOrDefault(cau.CauHoiID);
            var correctDa = correctMap.GetValueOrDefault(cau.CauHoiID);
            bool isCorrect;

            if (cau.LoaiCauHoi == "TraLoiNgan")
            {
                isCorrect = correctDa != null
                    && answer?.NoiDungTraLoi != null
                    && answer.NoiDungTraLoi.Trim().Equals(correctDa.NoiDung.Trim(), StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                isCorrect = answer?.DapAnID != null && answer.DapAnID == correctDa?.DapAnID;
            }

            if (isCorrect) correct++;

            results.Add(new QuestionResultDto
            {
                OrderIndex = item.ThuTu,
                CauHoiID = cau.CauHoiID,
                NoiDung = cau.NoiDung,
                LoaiCauHoi = cau.LoaiCauHoi,
                IsCorrect = isCorrect,
                SelectedDapAnID = answer?.DapAnID,
                CorrectDapAnID = correctDa?.DapAnID,
                GiaiThich = correctDa?.GiaiThich ?? cau.GiaiThichDapAn ?? string.Empty,
                HinhAnh = ToHinhAnhDto(cau.HinhAnhs),
                Options = cau.DapAns
                    .OrderBy(d => d.ThuTu)
                    .Select(d => new OptionResultApiDto
                    {
                        DapAnID = d.DapAnID,
                        ThuTu = d.ThuTu,
                        NoiDung = d.NoiDung,
                        Label = LabelOf(d.ThuTu),
                        IsCorrect = d.LaDapAnDung,
                        IsSelected = answer?.DapAnID == d.DapAnID,
                        HinhAnh = ToHinhAnhDto(d.HinhAnhs)
                    }).ToList()
            });
        }

        var total = results.Count;
        var score = total > 0 ? Math.Round((decimal)correct / total * 10, 2) : 0m;

        return Ok(new SubmitExamResultDto
        {
            DeThiID = id,
            Score = score,
            Total = total,
            Correct = correct,
            Wrong = total - correct,
            Results = results
        });
    }

    private static List<HinhAnhDto> ToHinhAnhDto(IEnumerable<HinhAnh> list) =>
        list.OrderBy(h => h.ThuTu).Select(h => new HinhAnhDto
        {
            HinhAnhID = h.HinhAnhID,
            ThuTu = h.ThuTu,
            Url = $"/api/hinhanh/{h.HinhAnhID}",
            MoTa = h.MoTa ?? string.Empty
        }).ToList();

    private static string LabelOf(int thuTu) => thuTu switch
    {
        0 => "A",
        1 => "B",
        2 => "C",
        3 => "D",
        _ => ((char)('A' + thuTu)).ToString()
    };

    private DeThiListItemDto ToListItemDto(DeThi d)
    {
        var dot = d.DotThis.FirstOrDefault();
        var trangThai = dot is null ? "DaDong"
            : dot.ThoiGianMoCong > DateTime.Now ? "SapMo"
            : dot.ThoiGianDongCong < DateTime.Now ? "DaDong"
            : "DangMo";

        return new DeThiListItemDto
        {
            DeThiID = d.DeThiID,
            TenDe = d.TenDe,
            TenMon = d.MonHoc?.TenMon ?? string.Empty,
            LoaiDe = d.LoaiDe,
            LoaiDeText = LoaiDeTextHelper.LoaiDeText(d.LoaiDe),
            SoLuongCauHoi = d.SoLuongCauHoi,
            HinhThucTao = d.HinhThucTao,
            DotThiID = dot?.DotThiID,
            TenDotThi = dot?.TenDotThi ?? string.Empty,
            ThoiGianMoCong = dot?.ThoiGianMoCong,
            ThoiGianDongCong = dot?.ThoiGianDongCong,
            ThoiLuongLamBai = dot?.ThoiLuongLamBai,
            TrangThai = trangThai
        };
    }
}