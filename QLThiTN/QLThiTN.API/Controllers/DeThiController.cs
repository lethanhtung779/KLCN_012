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
                        // Cau Dung/Sai: y gan nhan a b c d (chu thuong), khac voi A B C D
                        Label = q.CauHoi.LoaiCauHoi == "DungSai"
                            ? ((char)('a' + d.ThuTu)).ToString()
                            : LabelOf(d.ThuTu),
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

        var answerMap = request.Answers.ToDictionary(a => a.CauHoiID);

        var results = new List<QuestionResultDto>();
        decimal tongDiemDat = 0m, tongDiemToiDa = 0m;
        int correct = 0;

        foreach (var item in items)
        {
            var cau = item.CauHoi!;
            var answer = answerMap.GetValueOrDefault(cau.CauHoiID);
            var diemToiDa = cau.Diem > 0 ? cau.Diem : DiemMacDinh(cau.LoaiCauHoi);
            var diemDat = 0m;
            bool isCorrect;

            if (cau.LoaiCauHoi == "TraLoiNgan")
            {
                var correctDa = cau.DapAns.FirstOrDefault(d => d.LaDapAnDung);
                isCorrect = TraLoiNganDung(answer?.NoiDungTraLoi, correctDa?.NoiDung);
                if (isCorrect) diemDat = diemToiDa;

                results.Add(new QuestionResultDto
                {
                    OrderIndex = item.ThuTu,
                    CauHoiID = cau.CauHoiID,
                    NoiDung = cau.NoiDung,
                    LoaiCauHoi = cau.LoaiCauHoi,
                    IsCorrect = isCorrect,
                    GiaiThich = cau.GiaiThichDapAn ?? string.Empty,
                    HinhAnh = ToHinhAnhDto(cau.HinhAnhs),
                    NoiDungTraLoi = answer?.NoiDungTraLoi,
                    DapAnTraLoiNgan = correctDa?.NoiDung,
                    DiemDat = diemDat,
                    DiemToiDa = diemToiDa
                });
            }
            else if (cau.LoaiCauHoi == "DungSai")
            {
                // Cham theo tung y a-d: 0.25 diem moi y (diemToiDa / so y).
                var yRows = cau.DapAns.OrderBy(d => d.ThuTu).ToList();
                var chonMap = (answer?.YChoices ?? new List<SubmitYChoiceDto>())
                    .Where(y => y.DapAnID != 0)
                    .GroupBy(y => y.DapAnID)
                    .ToDictionary(g => g.Key, g => g.Last().LaDung);
                var diemMoiY = yRows.Count > 0 ? diemToiDa / yRows.Count : 0m;
                var yResults = new List<YResultDto>();
                int soYDung = 0;

                foreach (var y in yRows)
                {
                    var clientChon = chonMap.GetValueOrDefault(y.DapAnID);
                    var yDung = clientChon.HasValue && clientChon.Value == y.LaDapAnDung;
                    if (yDung) { soYDung++; diemDat += diemMoiY; }

                    yResults.Add(new YResultDto
                    {
                        DapAnID = y.DapAnID,
                        ThuTu = y.ThuTu,
                        Label = ((char)('a' + y.ThuTu)).ToString(),
                        NoiDung = y.NoiDung,
                        DapAnDung = y.LaDapAnDung,
                        ClientLaDung = clientChon,
                        IsCorrect = yDung,
                        HinhAnh = ToHinhAnhDto(y.HinhAnhs)
                    });
                }

                isCorrect = yRows.Count > 0 && soYDung == yRows.Count;

                results.Add(new QuestionResultDto
                {
                    OrderIndex = item.ThuTu,
                    CauHoiID = cau.CauHoiID,
                    NoiDung = cau.NoiDung,
                    LoaiCauHoi = cau.LoaiCauHoi,
                    IsCorrect = isCorrect,
                    GiaiThich = cau.GiaiThichDapAn ?? string.Empty,
                    HinhAnh = ToHinhAnhDto(cau.HinhAnhs),
                    YResults = yResults,
                    DiemDat = diemDat,
                    DiemToiDa = diemToiDa
                });
            }
            else // TracNghiem
            {
                var correctDa = cau.DapAns.FirstOrDefault(d => d.LaDapAnDung);
                isCorrect = answer?.DapAnID != null && correctDa != null
                    && answer.DapAnID == correctDa.DapAnID;
                if (isCorrect) diemDat = diemToiDa;

                results.Add(new QuestionResultDto
                {
                    OrderIndex = item.ThuTu,
                    CauHoiID = cau.CauHoiID,
                    NoiDung = cau.NoiDung,
                    LoaiCauHoi = cau.LoaiCauHoi,
                    IsCorrect = isCorrect,
                    SelectedDapAnID = answer?.DapAnID,
                    GiaiThich = correctDa?.GiaiThich ?? cau.GiaiThichDapAn ?? string.Empty,
                    HinhAnh = ToHinhAnhDto(cau.HinhAnhs),
                    DiemDat = diemDat,
                    DiemToiDa = diemToiDa,
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

            tongDiemDat += diemDat;
            tongDiemToiDa += diemToiDa;
            if (isCorrect) correct++;
        }

        var total = results.Count;
        var score = tongDiemToiDa > 0
            ? Math.Round(tongDiemDat / tongDiemToiDa * 10, 2, MidpointRounding.AwayFromZero)
            : 0m;

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

    private static decimal DiemMacDinh(string loai) => loai switch
    {
        "DungSai" => 1.00m,
        "TraLoiNgan" => 0.50m,
        _ => 0.25m
    };

    /// <summary>So sanh tra loi ngan: dung ca chuoi (khong phan biet hoa/thuong)
    /// va so (663 = 663,0 = 663.0).</summary>
    private static bool TraLoiNganDung(string? client, string? correct)
    {
        if (string.IsNullOrWhiteSpace(client) || string.IsNullOrWhiteSpace(correct))
            return false;
        var c = client.Trim();
        var k = correct.Trim();
        if (string.Equals(c, k, StringComparison.OrdinalIgnoreCase)) return true;

        if (decimal.TryParse(k.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var kd) &&
            decimal.TryParse(c.Replace(',', '.'),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var cd))
            return cd == kd;
        return false;
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